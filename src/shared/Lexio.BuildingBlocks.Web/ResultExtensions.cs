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
    /// <summary>Convert a failed Result to an IActionResult with appropriate HTTP status.</summary>
    public static IActionResult ToProblem(this Result result)
    {
        if (result.IsSuccess) { throw new InvalidOperationException("Cannot convert successful result to problem."); }

        var statusCode = result.Error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status500InternalServerError,
        };

        return new ObjectResult(new ProblemDetails
        {
            Status = statusCode,
            Title = result.Error.Code,
            Detail = result.Error.Message,
        })
        { StatusCode = statusCode };
    }
}
