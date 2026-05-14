using Lexio.Identity.Domain.ValueObjects;

namespace Lexio.Identity.Domain.Tests.ValueObjects;

public class DisplayNameTests
{
    [Fact]
    public void Create_trims_whitespace()
    {
        var result = DisplayName.Create("  alice  ");
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("alice");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_returns_failure_when_empty(string? raw)
    {
        DisplayName.Create(raw).IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Create_returns_failure_when_too_long()
    {
        DisplayName.Create(new string('a', 101)).IsFailure.Should().BeTrue();
    }

    [Theory]
    [InlineData("alice!")]
    [InlineData("user@home")]
    [InlineData("name<script>")]
    public void Create_returns_failure_on_disallowed_chars(string raw)
    {
        DisplayName.Create(raw).IsFailure.Should().BeTrue();
    }

    [Theory]
    [InlineData("Alice Smith")]
    [InlineData("alice_smith-1")]
    [InlineData("人 二 三")]   // unicode letters allowed
    public void Create_accepts_letters_digits_space_hyphen_underscore(string raw)
    {
        DisplayName.Create(raw).IsSuccess.Should().BeTrue();
    }
}
