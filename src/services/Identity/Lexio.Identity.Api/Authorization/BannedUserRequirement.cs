using Microsoft.AspNetCore.Authorization;

namespace Lexio.Identity.Api.Authorization;

/// <summary>Marker requirement for the "NotBanned" policy.</summary>
public sealed class BannedUserRequirement : IAuthorizationRequirement
{
    /// <summary>When true, the handler additionally consults the DB-backed ban cache.</summary>
    public bool RequiresDbCheck { get; init; }

    public static readonly BannedUserRequirement ClaimOnly = new() { RequiresDbCheck = false };
    public static readonly BannedUserRequirement WithDbCheck = new() { RequiresDbCheck = true };
}
