namespace Lexio.Identity.Application.Common.Authorization;

/// <summary>
/// Policy-name constants consumed by the Api layer when calling <c>AddAuthorization</c>.
/// Kept here (not in Api) so other handlers/tests can reference the same strings.
/// </summary>
public static class Policies
{
    public const string RequireLearner = "RequireLearner";
    public const string RequireVerifiedCreator = "RequireVerifiedCreator";
    public const string RequireModerator = "RequireModerator";
    public const string RequireAdmin = "RequireAdmin";
    public const string NotBanned = "NotBanned";

    /// <summary>Permission-policy prefix; concrete policy name is <c>perm:{permissionName}</c>.</summary>
    public const string PermissionPrefix = "perm:";

    public static string Permission(string name) => PermissionPrefix + name;
}

public static class Roles
{
    public const string Guest = "guest";
    public const string Learner = "learner";
    public const string VerifiedCreator = "verified-creator";
    public const string Moderator = "moderator";
    public const string Admin = "admin";
}

public static class ClaimTypes
{
    public const string Permissions = "permissions";
    public const string Banned = "banned";
}
