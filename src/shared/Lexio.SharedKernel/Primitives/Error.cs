namespace Lexio.SharedKernel.Primitives;

/// <summary>
/// Immutable error descriptor. Passed through Result to callers without throwing exceptions.
/// </summary>
/// <param name="Code">Machine-readable code (e.g. "User.NotFound").</param>
/// <param name="Message">Human-readable description.</param>
/// <param name="Type">Category used for HTTP status mapping.</param>
public sealed record Error(string Code, string Message, ErrorType Type)
{
    /// <summary>Sentinel value representing the absence of an error (used by Result.Success).</summary>
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.Internal);

    // ── Static factory helpers ────────────────────────────────────────────────

    public static Error Validation(string code, string message) =>
        new(code, message, ErrorType.Validation);

    public static Error NotFound(string code, string message) =>
        new(code, message, ErrorType.NotFound);

    public static Error Conflict(string code, string message) =>
        new(code, message, ErrorType.Conflict);

    public static Error Unauthorized(string code, string message) =>
        new(code, message, ErrorType.Unauthorized);

    public static Error Forbidden(string code, string message) =>
        new(code, message, ErrorType.Forbidden);

    public static Error Internal(string code, string message) =>
        new(code, message, ErrorType.Internal);
}
