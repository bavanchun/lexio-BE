namespace Lexio.Identity.Infrastructure.Security;

/// <summary>
/// Bound from the <c>OpenIddict</c> configuration section.
/// </summary>
public sealed class OpenIddictTokenIssuerOptions
{
    public const string SectionName = "OpenIddict";

    public string Issuer { get; set; } = "https://localhost/identity";
    public string Audience { get; set; } = "lexio-api";
    public int AccessTokenMinutes { get; set; } = 15;
    public int RefreshTokenDays { get; set; } = 7;
    public string? SigningCertPath { get; set; }
}
