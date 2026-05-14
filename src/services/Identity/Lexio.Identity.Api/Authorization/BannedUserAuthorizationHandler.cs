using System.Security.Claims;
using Lexio.Identity.Domain.Primitives;
using Microsoft.AspNetCore.Authorization;

namespace Lexio.Identity.Api.Authorization;

/// <summary>
/// Enforces the "NotBanned" policy. Reads the <c>banned</c> claim from the principal;
/// if true → fail. For requirements flagged <c>RequiresDbCheck</c>, additionally
/// consults <see cref="BanStatusCache"/> (60s TTL) so a freshly-banned user is
/// blocked from write endpoints within one request after admin action.
/// </summary>
public sealed class BannedUserAuthorizationHandler(BanStatusCache cache)
    : AuthorizationHandler<BannedUserRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        BannedUserRequirement requirement)
    {
        if (ReadBannedClaim(context.User))
        {
            context.Fail(new AuthorizationFailureReason(this, "banned"));
            return;
        }

        if (requirement.RequiresDbCheck
            && TryReadUserId(context.User, out var userId)
            && await cache.IsBannedAsync(userId).ConfigureAwait(false))
        {
            context.Fail(new AuthorizationFailureReason(this, "banned"));
            return;
        }

        context.Succeed(requirement);
    }

    private static bool ReadBannedClaim(ClaimsPrincipal user)
    {
        var raw = user.FindFirst("banned")?.Value;
        return bool.TryParse(raw, out var banned) && banned;
    }

    private static bool TryReadUserId(ClaimsPrincipal user, out UserId id)
    {
        var sub = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? user.FindFirst("sub")?.Value;
        if (Guid.TryParse(sub, out var guid))
        {
            id = new UserId(guid);
            return true;
        }
        id = default;
        return false;
    }
}
