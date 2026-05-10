namespace Lexio.BuildingBlocks.Abstractions.Auth;

/// <summary>
/// Provides identity information for the current authenticated user.
/// In HTTP context this reads from IHttpContextAccessor.HttpContext.User (phase 05 impl).
/// In background jobs, a test-double or system-user impl is injected instead.
/// </summary>
public interface ICurrentUserAccessor
{
    /// <summary>The user's unique identifier; null for anonymous requests.</summary>
    Guid? UserId { get; }

    /// <summary>The user's email address; null for anonymous requests.</summary>
    string? Email { get; }

    /// <summary>All roles assigned to the user; empty for anonymous requests.</summary>
    IReadOnlyList<string> Roles { get; }

    /// <summary>True when a user is authenticated.</summary>
    bool IsAuthenticated { get; }
}
