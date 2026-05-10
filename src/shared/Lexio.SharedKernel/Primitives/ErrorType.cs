namespace Lexio.SharedKernel.Primitives;

/// <summary>
/// Discriminated union of error categories. Used by Result to derive HTTP status codes
/// in the Web building block (Validation→400, NotFound→404, Conflict→409, etc.).
/// </summary>
public enum ErrorType
{
    Validation = 0,
    NotFound = 1,
    Conflict = 2,
    Unauthorized = 3,
    Forbidden = 4,
    Internal = 5,
}
