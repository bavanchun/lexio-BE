using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Lexio.Identity.Application.Contracts.Security;
using Lexio.Identity.Domain.Entities;
using Lexio.SharedKernel.Time;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Lexio.Identity.Infrastructure.Security;

/// <summary>
/// Mints RS256 access tokens signed by the shared <see cref="SigningCertificateLoader"/>
/// and CSPRNG-random refresh tokens. Refresh-token persistence + rotation is owned
/// by the application layer (handlers call <see cref="ComputeStorageHash"/> for lookup);
/// this type is stateless apart from the signing key.
/// </summary>
public sealed class OpenIddictTokenIssuer : ITokenIssuer
{
    private readonly IClock _clock;
    private readonly OpenIddictTokenIssuerOptions _opts;
    private readonly SigningCredentials _signing;

    public OpenIddictTokenIssuer(
        IClock clock,
        IOptions<OpenIddictTokenIssuerOptions> options,
        SigningCertificateLoader certLoader)
    {
        _clock = clock;
        _opts = options.Value;

        var key = new X509SecurityKey(certLoader.Certificate);
        _signing = new SigningCredentials(key, SecurityAlgorithms.RsaSha256);
    }

    public IssuedAccessToken IssueAccessToken(User user, Role role)
    {
        var now = _clock.UtcNow;
        var expires = now.AddMinutes(_opts.AccessTokenMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.Value.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email.Value),
            new(JwtRegisteredClaimNames.Name, user.DisplayName.Value),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new("role", role.Name),
            new("banned", (user.Status == Domain.Enums.UserStatus.Banned).ToString().ToLowerInvariant()),
        };

        foreach (var permission in role.Permissions)
        {
            claims.Add(new Claim("permissions", permission));
        }

        var jwt = new JwtSecurityToken(
            issuer: _opts.Issuer,
            audience: _opts.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expires.UtcDateTime,
            signingCredentials: _signing);

        var encoded = new JwtSecurityTokenHandler().WriteToken(jwt);
        var expiresIn = (int)(expires - now).TotalSeconds;
        return new IssuedAccessToken(encoded, expires, expiresIn);
    }

    public IssuedRefreshToken IssueRefreshToken()
    {
        var raw = RefreshTokenGenerator.NewRawToken();
        var expires = _clock.UtcNow.AddDays(_opts.RefreshTokenDays);
        return new IssuedRefreshToken(raw, ComputeStorageHash(raw), expires);
    }

    public string ComputeStorageHash(string rawToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(rawToken);
        Span<byte> digest = stackalloc byte[SHA256.HashSizeInBytes];
        SHA256.HashData(Encoding.UTF8.GetBytes(rawToken), digest);
        return Base64UrlEncoder.Encode(digest.ToArray());
    }
}
