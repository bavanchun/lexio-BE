namespace Lexio.Identity.Domain.Primitives;

/// <summary>Strongly-typed identifier for <see cref="Entities.OAuthConnection"/>.</summary>
public readonly record struct OAuthConnectionId(Guid Value)
{
    public static OAuthConnectionId New() => new(Guid.NewGuid());
    public static OAuthConnectionId Empty { get; } = new(Guid.Empty);
    public override string ToString() => Value.ToString();
    public static implicit operator Guid(OAuthConnectionId id) => id.Value;
}
