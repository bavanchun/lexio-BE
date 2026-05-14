using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;

namespace Lexio.Identity.Infrastructure.Security;

/// <summary>
/// 32-byte CSPRNG refresh tokens encoded base64url (43 chars, unpadded).
/// </summary>
public static class RefreshTokenGenerator
{
    public const int RawByteLength = 32;

    public static string NewRawToken()
    {
        Span<byte> buf = stackalloc byte[RawByteLength];
        RandomNumberGenerator.Fill(buf);
        return Base64UrlEncoder.Encode(buf.ToArray());
    }
}
