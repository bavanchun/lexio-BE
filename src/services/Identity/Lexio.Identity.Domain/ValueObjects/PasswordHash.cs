using Lexio.Identity.Domain.Exceptions;
using Lexio.SharedKernel.Domain;
using Lexio.SharedKernel.Primitives;

namespace Lexio.Identity.Domain.ValueObjects;

/// <summary>
/// Holds an already-bcrypt-hashed password. Constructor refuses anything that
/// doesn't carry a bcrypt prefix ($2a$ / $2b$ / $2y$) so plaintext can never
/// be persisted by accident.
/// </summary>
public sealed class PasswordHash : ValueObject
{
    private static readonly string[] BcryptPrefixes = ["$2a$", "$2b$", "$2y$"];

    public string Value { get; }

    private PasswordHash(string value) => Value = value;

    public static Result<PasswordHash> Create(string? hash)
    {
        if (string.IsNullOrWhiteSpace(hash))
        {
            return Result.Failure<PasswordHash>(Error.Validation("password_hash.empty", "Password hash is required."));
        }

        if (!BcryptPrefixes.Any(p => hash.StartsWith(p, StringComparison.Ordinal)))
        {
            return Result.Failure<PasswordHash>(Error.Validation(
                "password_hash.format",
                "Password hash must be a bcrypt hash ($2a$/$2b$/$2y$ prefix)."));
        }

        return Result.Success(new PasswordHash(hash));
    }

    public static PasswordHash From(string hash)
    {
        var result = Create(hash);
        if (result.IsFailure)
        {
            throw new InvalidPasswordHashException(result.Error.Message);
        }
        return result.Value;
    }

    protected override IEnumerable<object?> GetEqualityComponents() { yield return Value; }

    public override string ToString() => "[redacted password hash]";
}
