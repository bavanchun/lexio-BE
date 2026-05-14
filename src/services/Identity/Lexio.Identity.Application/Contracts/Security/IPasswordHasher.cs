namespace Lexio.Identity.Application.Contracts.Security;

public interface IPasswordHasher
{
    /// <summary>Hashes a plaintext password with the configured bcrypt work factor.</summary>
    string Hash(string plaintext);

    /// <summary>
    /// Constant-time verify. Implementations MUST execute the same bcrypt work even when
    /// <paramref name="hash"/> is null/empty so user-existence cannot be inferred from timing.
    /// </summary>
    bool Verify(string? plaintext, string? hash);
}
