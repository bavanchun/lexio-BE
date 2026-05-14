namespace Lexio.Identity.Domain.Primitives;

/// <summary>Strongly-typed identifier for <see cref="Entities.RefreshToken"/>.</summary>
public readonly record struct RefreshTokenId(Guid Value)
{
    public static RefreshTokenId New() => new(Guid.NewGuid());
    public static RefreshTokenId Empty { get; } = new(Guid.Empty);
    public override string ToString() => Value.ToString();
    public static implicit operator Guid(RefreshTokenId id) => id.Value;
}
