using System.Security.Claims;
using Lexio.BuildingBlocks.Abstractions.Auth;
using Microsoft.AspNetCore.Http;

namespace Lexio.BuildingBlocks.Auth;

/// <summary>
/// Reads identity claims from the current HttpContext.User.
/// Registered as Scoped — one instance per HTTP request.
/// In background-job contexts, inject a system-user test double instead.
/// </summary>
internal sealed class CurrentUserAccessor(IHttpContextAccessor httpContextAccessor) : ICurrentUserAccessor
{
    private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

    public Guid? UserId
    {
        get
        {
            var sub = User?.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User?.FindFirstValue("sub");
            return Guid.TryParse(sub, out var id) ? id : null;
        }
    }

    public string? Email => User?.FindFirstValue(ClaimTypes.Email)
        ?? User?.FindFirstValue("email");

    public IReadOnlyList<string> Roles =>
        User?.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList().AsReadOnly()
        ?? (IReadOnlyList<string>)[];

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated is true;
}
