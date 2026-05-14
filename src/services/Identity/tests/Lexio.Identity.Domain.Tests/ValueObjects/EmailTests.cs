using Lexio.Identity.Domain.ValueObjects;

namespace Lexio.Identity.Domain.Tests.ValueObjects;

public class EmailTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_returns_failure_when_empty(string? raw)
    {
        var result = Email.Create(raw);
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("email.empty");
    }

    [Theory]
    [InlineData("a@b")]                    // too short (<5)
    [InlineData("no-at-sign")]
    [InlineData("two@@signs.com")]
    public void Create_returns_failure_on_bad_format(string raw)
    {
        var result = Email.Create(raw);
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Create_normalises_to_trimmed_lowercase()
    {
        var result = Email.Create("  Foo@Bar.COM  ");
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("foo@bar.com");
    }

    [Fact]
    public void Equality_is_case_insensitive_via_normalisation()
    {
        var a = Email.From("user@example.com");
        var b = Email.From("USER@EXAMPLE.COM");
        a.Should().Be(b);
    }

    [Fact]
    public void From_throws_on_invalid_input()
    {
        var act = () => Email.From("nope");
        act.Should().Throw<Exception>();
    }
}
