using System.Text.RegularExpressions;
using Lexio.Identity.Domain.Exceptions;
using Lexio.SharedKernel.Domain;
using Lexio.SharedKernel.Primitives;

namespace Lexio.Identity.Domain.ValueObjects;

/// <summary>
/// Public-facing display name. 1-100 chars; alphanumerics + space/hyphen/underscore.
/// Trim-validated.
/// </summary>
public sealed partial class DisplayName : ValueObject
{
    public const int MinLength = 1;
    public const int MaxLength = 100;

    public string Value { get; }

    private DisplayName(string value) => Value = value;

    public static Result<DisplayName> Create(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return Result.Failure<DisplayName>(Error.Validation("display_name.empty", "Display name is required."));
        }

        var trimmed = raw.Trim();

        if (trimmed.Length < MinLength || trimmed.Length > MaxLength)
        {
            return Result.Failure<DisplayName>(Error.Validation(
                "display_name.length",
                $"Display name must be {MinLength}-{MaxLength} characters."));
        }

        if (!DisplayNameRegex().IsMatch(trimmed))
        {
            return Result.Failure<DisplayName>(Error.Validation(
                "display_name.format",
                "Display name may contain only letters, digits, spaces, hyphens, or underscores."));
        }

        return Result.Success(new DisplayName(trimmed));
    }

    public static DisplayName From(string raw)
    {
        var result = Create(raw);
        if (result.IsFailure)
        {
            throw new InvalidDisplayNameException(result.Error.Message);
        }
        return result.Value;
    }

    protected override IEnumerable<object?> GetEqualityComponents() { yield return Value; }

    public override string ToString() => Value;

    [GeneratedRegex(@"^[\p{L}\p{N} _-]+$", RegexOptions.CultureInvariant)]
    private static partial Regex DisplayNameRegex();
}
