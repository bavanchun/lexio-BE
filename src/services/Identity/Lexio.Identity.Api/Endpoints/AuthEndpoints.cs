using System.Security.Claims;
using Lexio.BuildingBlocks.Web;
using Lexio.Identity.Api.Configuration;
using Lexio.Identity.Application.Features.Auth.Login;
using Lexio.Identity.Application.Features.Auth.Logout;
using Lexio.Identity.Application.Features.Auth.Me;
using Lexio.Identity.Application.Features.Auth.Refresh;
using Lexio.Identity.Application.Features.Auth.Register;
using Lexio.Identity.Domain.Primitives;
using Mediator;

namespace Lexio.Identity.Api.Endpoints;

public static class AuthEndpoints
{
    public sealed record RegisterRequest(string Email, string Password, string DisplayName);
    public sealed record LoginRequest(string Email, string Password);
    public sealed record RefreshRequest(string RefreshToken);

    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/auth").WithTags("Auth");

        group.MapPost("/register", async (RegisterRequest body, IMediator mediator, HttpContext http) =>
        {
            var result = await mediator.Send(new RegisterUserCommand(
                body.Email, body.Password, body.DisplayName, ClientIp(http)));
            return result.Match(dto => Results.Created("/api/v1/auth/me", dto));
        })
        .RequireRateLimiting(RateLimitingExtensions.Login)
        .AllowAnonymous();

        group.MapPost("/login", async (LoginRequest body, IMediator mediator, HttpContext http) =>
        {
            var result = await mediator.Send(new LoginCommand(body.Email, body.Password, ClientIp(http)));
            return result.Match(dto => Results.Ok(dto));
        })
        .RequireRateLimiting(RateLimitingExtensions.Login)
        .AllowAnonymous();

        group.MapPost("/refresh", async (RefreshRequest body, IMediator mediator, HttpContext http) =>
        {
            var result = await mediator.Send(new RefreshTokenCommand(body.RefreshToken, ClientIp(http)));
            return result.Match(dto => Results.Ok(dto));
        })
        .RequireRateLimiting(RateLimitingExtensions.Login)
        .AllowAnonymous();

        group.MapPost("/logout", async (IMediator mediator, ClaimsPrincipal user) =>
        {
            if (!TryReadUserId(user, out var userId)) { return Results.Unauthorized(); }
            var result = await mediator.Send(new LogoutCommand(userId));
            return result.Match(() => Results.NoContent());
        })
        .RequireRateLimiting(RateLimitingExtensions.Authenticated)
        .RequireAuthorization(AuthorizationExtensions.NotBannedPolicy);

        group.MapGet("/me", async (IMediator mediator, ClaimsPrincipal user) =>
        {
            if (!TryReadUserId(user, out var userId)) { return Results.Unauthorized(); }
            var result = await mediator.Send(new GetMeQuery(userId));
            return result.Match(dto => Results.Ok(dto));
        })
        .RequireRateLimiting(RateLimitingExtensions.Authenticated)
        .RequireAuthorization(AuthorizationExtensions.NotBannedPolicy);

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

    private static string? ClientIp(HttpContext ctx) =>
        ctx.Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',')[0].Trim()
        ?? ctx.Connection.RemoteIpAddress?.ToString();
}
