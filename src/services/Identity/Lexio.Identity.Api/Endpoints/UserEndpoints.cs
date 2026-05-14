using System.Security.Claims;
using Lexio.BuildingBlocks.Web;
using Lexio.Identity.Api.Configuration;
using Lexio.Identity.Application.Features.Users.ChangeRole;
using Lexio.Identity.Application.Features.Users.UpdateProfile;
using Lexio.Identity.Domain.Primitives;
using Mediator;

namespace Lexio.Identity.Api.Endpoints;

public static class UserEndpoints
{
    public sealed record UpdateProfileRequest(string? DisplayName, string? CurrentPassword, string? NewPassword);
    public sealed record ChangeRoleRequest(Guid NewRoleId);

    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/users").WithTags("Users");

        group.MapPut("/me", async (UpdateProfileRequest body, IMediator mediator, ClaimsPrincipal user) =>
        {
            if (!TryReadUserId(user, out var userId)) { return Results.Unauthorized(); }
            var result = await mediator.Send(new UpdateProfileCommand(
                userId, body.DisplayName, body.CurrentPassword, body.NewPassword));
            return result.Match(dto => Results.Ok(dto));
        })
        .RequireRateLimiting(RateLimitingExtensions.Authenticated)
        .RequireAuthorization(AuthorizationExtensions.NotBannedWithDbCheckPolicy);

        group.MapPost("/{id:guid}/role", async (
            Guid id, ChangeRoleRequest body, IMediator mediator, ClaimsPrincipal user) =>
        {
            if (!TryReadUserId(user, out var adminId)) { return Results.Unauthorized(); }
            var result = await mediator.Send(new ChangeUserRoleCommand(
                new UserId(id), new RoleId(body.NewRoleId), adminId));
            return result.Match(() => Results.NoContent());
        })
        .RequireRateLimiting(RateLimitingExtensions.Authenticated)
        .RequireAuthorization(AuthorizationExtensions.NotBannedWithDbCheckPolicy)
        .RequireAuthorization(AuthorizationExtensions.AdminPolicy);

        return app;
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
