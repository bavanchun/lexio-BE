namespace Lexio.BuildingBlocks.Auth;

/// <summary>
/// Well-known authorization policy names used across all Lexio services.
/// Register these policies via AddLexioAuth and reference them in [Authorize(Policy=...)].
/// </summary>
public static class AuthorizationPolicies
{
    public const string RequireUser = "RequireUser";
    public const string RequireAdmin = "RequireAdmin";
}
