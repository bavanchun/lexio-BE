namespace Lexio.Identity.Domain;

/// <summary>
/// Marker type used by architecture tests to reference the Domain assembly.
/// Do not delete — NetArchTest uses this to load the assembly.
/// </summary>
// S2094: empty class is intentional — it is a compile-time assembly anchor, not a real class.
#pragma warning disable S2094
public static class DomainAnchor { }
#pragma warning restore S2094
