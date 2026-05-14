using Lexio.BuildingBlocks.Web;
using Lexio.Identity.Api.Configuration;
using Lexio.Identity.Application.Features.Roles.List;
using Mediator;

namespace Lexio.Identity.Api.Endpoints;

public static class RoleEndpoints
{
    public static IEndpointRouteBuilder MapRoleEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/roles", async (IMediator mediator) =>
        {
            var result = await mediator.Send(new GetRolesQuery());
            return result.Match(roles => Results.Ok(roles));
        })
        .WithTags("Roles")
        .RequireRateLimiting(RateLimitingExtensions.Default)
        .AllowAnonymous();

        return app;
    }
}
