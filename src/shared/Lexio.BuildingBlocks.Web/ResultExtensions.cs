using Lexio.SharedKernel.Primitives;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Lexio.BuildingBlocks.Web;

/// <summary>
/// Maps a Result.Error to the corresponding HTTP status code and ProblemDetails.
/// Used by minimal API endpoints and controller action filters.
/// </summary>
public static class ResultExtensions
{
    public const string ProblemTypeBase = "https://api.lexio.dev/errors/";

    /// <summary>Convert a failed Result to an IActionResult with appropriate HTTP status.</summary>
    public static IActionResult ToProblem(this Result result)
    {
        if (result.IsSuccess) { throw new InvalidOperationException("Cannot convert successful result to problem."); }
        var status = StatusFor(result.Error.Type);
        return new ObjectResult(BuildProblem(result.Error, status)) { StatusCode = status };
    }

    /// <summary>Convert a failed Result to a minimal-API IResult with appropriate HTTP status.</summary>
    public static IResult ToHttpResult(this Result result)
    {
        if (result.IsSuccess) { throw new InvalidOperationException("Cannot convert successful result to problem."); }
        var status = StatusFor(result.Error.Type);
        return Results.Problem(BuildProblem(result.Error, status));
    }

    /// <summary>Map a generic Result&lt;T&gt;: success → <paramref name="onSuccess"/>; failure → ProblemDetails.</summary>
    public static IResult Match<T>(this Result<T> result, Func<T, IResult> onSuccess) =>
        result.IsSuccess ? onSuccess(result.Value) : Results.Problem(BuildProblem(result.Error, StatusFor(result.Error.Type)));

    public static IResult Match(this Result result, Func<IResult> onSuccess) =>
        result.IsSuccess ? onSuccess() : Results.Problem(BuildProblem(result.Error, StatusFor(result.Error.Type)));

    private static int StatusFor(ErrorType type) => type switch
    {
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorType.Forbidden => StatusCodes.Status403Forbidden,
        _ => StatusCodes.Status500InternalServerError,
    };

    private static ProblemDetails BuildProblem(Error error, int status) => new()
    {
        Status = status,
        Title = error.Code,
        Detail = error.Message,
        Type = ProblemTypeBase + error.Code,
    };
}
