using FluentAssertions;
using Lexio.SharedKernel.Primitives;

namespace Lexio.SharedKernel.Tests;

public sealed class MaybeTests
{
    [Fact]
    public void Maybe_From_NonNull_HasValue()
    {
        var maybe = Maybe<string>.From("hello");

        maybe.HasValue.Should().BeTrue();
        maybe.HasNoValue.Should().BeFalse();
        maybe.Value.Should().Be("hello");
    }

    [Fact]
    public void Maybe_From_Null_HasNoValue()
    {
        var maybe = Maybe<string>.From(null);

        maybe.HasNoValue.Should().BeTrue();
        maybe.HasValue.Should().BeFalse();
    }

    [Fact]
    public void Maybe_Value_WhenEmpty_ThrowsInvalidOperation()
    {
        var maybe = Maybe<int>.None;

        var act = () => maybe.Value;
        act.Should().Throw<InvalidOperationException>().WithMessage("Maybe has no value.");
    }

    [Fact]
    public void Maybe_ImplicitConversion_FromNonNull_HasValue()
    {
        Maybe<int> maybe = 42;

        maybe.HasValue.Should().BeTrue();
        maybe.Value.Should().Be(42);
    }

    [Fact]
    public void Maybe_ImplicitConversion_FromNull_HasNoValue()
    {
        Maybe<string> maybe = (string?)null;

        maybe.HasNoValue.Should().BeTrue();
    }

    [Fact]
    public void Maybe_ToString_WithValue_ReturnsValueString()
    {
        Maybe<int> maybe = 7;
        maybe.ToString().Should().Be("7");
    }

    [Fact]
    public void Maybe_ToString_Empty_ReturnsNone()
    {
        var maybe = Maybe<string>.None;
        maybe.ToString().Should().Be("None");
    }
}
