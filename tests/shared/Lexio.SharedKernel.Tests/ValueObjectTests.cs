using FluentAssertions;
using Lexio.SharedKernel.Domain;

namespace Lexio.SharedKernel.Tests;

public sealed class ValueObjectTests
{
    // ── Test double ───────────────────────────────────────────────────────────

    private sealed class Money(decimal amount, string currency) : ValueObject
    {
        public decimal Amount { get; } = amount;
        public string Currency { get; } = currency;

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Amount;
            yield return Currency;
        }
    }

    private sealed class Address(string street, string city) : ValueObject
    {
        public string Street { get; } = street;
        public string City { get; } = city;

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Street;
            yield return City;
        }
    }

    // ── Structural equality ───────────────────────────────────────────────────

    [Fact]
    public void ValueObjects_WithSameComponents_AreEqual()
    {
        var a = new Money(100m, "USD");
        var b = new Money(100m, "USD");

        a.Should().Be(b);
        (a == b).Should().BeTrue();
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void ValueObjects_WithDifferentComponents_AreNotEqual()
    {
        var a = new Money(100m, "USD");
        var b = new Money(200m, "USD");

        a.Should().NotBe(b);
        (a != b).Should().BeTrue();
    }

    [Fact]
    public void ValueObjects_OfDifferentTypes_AreNotEqual()
    {
        // Money and Address both extend ValueObject but are different types
        ValueObject money = new Money(0m, "X");
        ValueObject address = new Address("X", "Y");

        money.Should().NotBe(address);
    }

    [Fact]
    public void ValueObject_Equals_Null_ReturnsFalse()
    {
        var vo = new Money(1m, "EUR");
        vo.Equals(null).Should().BeFalse();
    }

    [Fact]
    public void ValueObject_ComponentOrder_Matters()
    {
        // Swapped components must not be equal even if same values
        var a = new Address("Main St", "Springfield");
        var b = new Address("Springfield", "Main St");

        a.Should().NotBe(b);
    }

    [Fact]
    public void ValueObject_WithNullComponent_HandledGracefully()
    {
        var a = new Address("Main St", null!);
        var b = new Address("Main St", null!);

        a.Should().Be(b);
    }
}
