using FluentAssertions;
using Lexio.SharedKernel.Domain;

namespace Lexio.SharedKernel.Tests;

public sealed class EntityTests
{
    // ── Concrete test doubles ─────────────────────────────────────────────────

    private sealed class PersonId(Guid value)
    {
        public Guid Value { get; } = value;
        public override bool Equals(object? obj) => obj is PersonId other && Value == other.Value;
        public override int GetHashCode() => Value.GetHashCode();
    }

    private sealed class Person(PersonId id) : Entity<PersonId>(id) { }

    private sealed class Order(PersonId id) : Entity<PersonId>(id) { }

    // ── Equality tests ────────────────────────────────────────────────────────

    [Fact]
    public void Entities_WithSameId_AreEqual()
    {
        var id = new PersonId(Guid.NewGuid());
        var a = new Person(id);
        var b = new Person(id);

        a.Should().Be(b);
        (a == b).Should().BeTrue();
        (a != b).Should().BeFalse();
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void Entities_WithDifferentIds_AreNotEqual()
    {
        var a = new Person(new PersonId(Guid.NewGuid()));
        var b = new Person(new PersonId(Guid.NewGuid()));

        a.Should().NotBe(b);
        (a == b).Should().BeFalse();
    }

    [Fact]
    public void Entities_OfDifferentTypes_WithSameId_AreNotEqual()
    {
        // Same raw Guid, different entity types — must not be equal
        var sharedId = new PersonId(Guid.NewGuid());
        Entity<PersonId> person = new Person(sharedId);
        Entity<PersonId> order = new Order(sharedId);

        person.Should().NotBe(order);
        (person == order).Should().BeFalse();
    }

    [Fact]
    public void Entity_Equals_Null_ReturnsFalse()
    {
        var person = new Person(new PersonId(Guid.NewGuid()));
        person.Equals(null).Should().BeFalse();
    }

    [Fact]
    public void Entity_ReferenceEquals_ReturnsTrueWithoutIdComparison()
    {
        var person = new Person(new PersonId(Guid.NewGuid()));
#pragma warning disable CS1718 // Intentional self-comparison
        (person == person).Should().BeTrue();
#pragma warning restore CS1718
    }
}
