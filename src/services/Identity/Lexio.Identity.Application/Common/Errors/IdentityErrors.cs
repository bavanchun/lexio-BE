using Lexio.SharedKernel.Primitives;

namespace Lexio.Identity.Application.Common.Errors;

/// <summary>Centralised <see cref="Error"/> catalogue. Codes map 1:1 to API error types.</summary>
public static class IdentityErrors
{
    public static Error EmailAlreadyExists =>
        Error.Conflict("identity.email-already-exists", "An account with this email already exists.");

    public static Error InvalidCredentials =>
        Error.Unauthorized("identity.invalid-credentials", "Invalid email or password.");

    public static Error AccountBanned =>
        Error.Forbidden("identity.account-banned", "This account is banned.");

    public static Error UserNotFound =>
        Error.NotFound("identity.user-not-found", "User not found.");

    public static Error RoleNotFound =>
        Error.NotFound("identity.role-not-found", "Role not found.");

    public static Error InvalidRefreshToken =>
        Error.Unauthorized("identity.invalid-refresh-token", "Refresh token is invalid or expired.");

    public static Error CurrentPasswordRequired =>
        Error.Validation("identity.current-password-required", "Current password is required to change password.");

    public static Error CurrentPasswordMismatch =>
        Error.Unauthorized("identity.current-password-mismatch", "Current password is incorrect.");

    public static Error PasswordNotSet =>
        Error.Validation("identity.password-not-set", "This account has no password (external login). Cannot change password directly.");
}
