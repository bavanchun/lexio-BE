using Lexio.Identity.Application.Contracts.Security;

namespace Lexio.Identity.Infrastructure.Security;

/// <summary>
/// bcrypt-backed <see cref="IPasswordHasher"/>. Verify always performs a full bcrypt
/// round (against <see cref="DummyHash"/> when input is missing/malformed) so a
/// caller cannot distinguish "user not found" from "wrong password" via timing.
/// </summary>
public sealed class BCryptPasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 12;

    // Generated once per process. Plaintext is arbitrary — only the bcrypt-work-time matters.
    private static readonly string DummyHash =
        BCrypt.Net.BCrypt.HashPassword("__lexio_dummy__", WorkFactor);

    public string Hash(string plaintext)
    {
        ArgumentException.ThrowIfNullOrEmpty(plaintext);
        return BCrypt.Net.BCrypt.HashPassword(plaintext, WorkFactor);
    }

    public bool Verify(string? plaintext, string? hash)
    {
        if (string.IsNullOrEmpty(plaintext) || string.IsNullOrEmpty(hash))
        {
            _ = SafeVerify(plaintext ?? string.Empty, DummyHash);
            return false;
        }

        try
        {
            return BCrypt.Net.BCrypt.Verify(plaintext, hash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            _ = SafeVerify(plaintext, DummyHash);
            return false;
        }
    }

    private static bool SafeVerify(string plaintext, string hash)
    {
        try { return BCrypt.Net.BCrypt.Verify(plaintext, hash); }
        catch { return false; }
    }
}
