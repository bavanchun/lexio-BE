using FluentAssertions;
using Lexio.SharedKernel.Primitives;

namespace Lexio.SharedKernel.Tests;

public sealed class ResultTests
{
    // ── Non-generic Result ────────────────────────────────────────────────────

    [Fact]
    public void Result_Success_IsSuccess_True()
    {
        var result = Result.Success();

        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Error.Should().Be(Error.None);
    }

    [Fact]
    public void Result_Failure_IsFailure_True()
    {
        var error = Error.NotFound("User.NotFound", "User not found");
        var result = Result.Failure(error);

        result.IsFailure.Should().BeTrue();
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(error);
    }

    [Fact]
    public void Result_Success_WithErrorNonNone_Throws()
    {
        var error = Error.Internal("X", "X");
        var act = () => new ExposedResult(true, error);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Result_Failure_WithErrorNone_Throws()
    {
        var act = () => new ExposedResult(false, Error.None);

        act.Should().Throw<ArgumentException>();
    }

    // ── Generic Result<T> ─────────────────────────────────────────────────────

    [Fact]
    public void ResultOfT_Success_ExposesValue()
    {
        var result = Result.Success(42);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
    }

    [Fact]
    public void ResultOfT_Failure_AccessingValue_ThrowsInvalidOperation()
    {
        var error = Error.Validation("Name.Empty", "Name is required");
        var result = Result.Failure<string>(error);

        result.IsFailure.Should().BeTrue();
        var act = () => result.Value;
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Name.Empty*");
    }

    [Fact]
    public void ResultOfT_ImplicitConversionFromValue_CreatesSuccess()
    {
        Result<int> result = 99;

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(99);
    }

    [Fact]
    public void ResultOfT_ImplicitConversionFromError_CreatesFailure()
    {
        var error = Error.Conflict("Duplicate", "Already exists");
        Result<int> result = error;

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(error);
    }

    [Fact]
    public void Error_StaticHelpers_SetCorrectType()
    {
        Error.Validation("v", "v").Type.Should().Be(ErrorType.Validation);
        Error.NotFound("n", "n").Type.Should().Be(ErrorType.NotFound);
        Error.Conflict("c", "c").Type.Should().Be(ErrorType.Conflict);
        Error.Unauthorized("u", "u").Type.Should().Be(ErrorType.Unauthorized);
        Error.Forbidden("f", "f").Type.Should().Be(ErrorType.Forbidden);
        Error.Internal("i", "i").Type.Should().Be(ErrorType.Internal);
    }

    // ── Test helper: exposes protected Result ctor ────────────────────────────
    private sealed class ExposedResult(bool isSuccess, Error error)
        : Result(isSuccess, error);
}
