namespace Lexio.Identity.Domain.Primitives;

/// <summary>Strongly-typed identifier for <see cref="Entities.User"/>.</summary>
public readonly record struct UserId(Guid Value)
{
    public static UserId New() => new(Guid.NewGuid());
    public static UserId Empty { get; } = new(Guid.Empty);
    public override string ToString() => Value.ToString();
    public static implicit operator Guid(UserId id) => id.Value;
}
