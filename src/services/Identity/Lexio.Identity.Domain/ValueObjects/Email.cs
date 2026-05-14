using System.Text.RegularExpressions;
using Lexio.Identity.Domain.Exceptions;
using Lexio.SharedKernel.Domain;
using Lexio.SharedKernel.Primitives;

namespace Lexio.Identity.Domain.ValueObjects;

/// <summary>
/// RFC-5321-ish email. Normalised to trim+lowercase so equality + DB unique index agree.
/// Backing DB column must be unique on the lowercased form.
/// </summary>
public sealed partial class Email : ValueObject
{
    public const int MaxLength = 255;
    public const int MinLength = 5;

    public string Value { get; }

    private Email(string value) => Value = value;

    public static Result<Email> Create(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return Result.Failure<Email>(Error.Validation("email.empty", "Email is required."));
        }

        var normalised = raw.Trim().ToLowerInvariant();

        if (normalised.Length < MinLength || normalised.Length > MaxLength)
        {
            return Result.Failure<Email>(Error.Validation("email.length", $"Email must be {MinLength}-{MaxLength} characters."));
        }

        if (!EmailRegex().IsMatch(normalised))
        {
            return Result.Failure<Email>(Error.Validation("email.format", "Email format is invalid."));
        }

        return Result.Success(new Email(normalised));
    }

    /// <summary>Throws <see cref="InvalidEmailException"/> on validation failure. Use sparingly — prefer <see cref="Create"/>.</summary>
    public static Email From(string raw)
    {
        var result = Create(raw);
        if (result.IsFailure)
        {
            throw new InvalidEmailException(result.Error.Message);
        }
        return result.Value;
    }

    protected override IEnumerable<object?> GetEqualityComponents() { yield return Value; }

    public override string ToString() => Value;

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.CultureInvariant)]
    private static partial Regex EmailRegex();
}
