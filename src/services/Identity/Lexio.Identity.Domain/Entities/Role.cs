using Lexio.Identity.Domain.Primitives;
using Lexio.SharedKernel.Domain;

namespace Lexio.Identity.Domain.Entities;

/// <summary>
/// RBAC role. Five well-known roles (Guest, Learner, Verified Creator, Moderator, Admin). Permissions stored as a
/// flat string list.
/// </summary>
public sealed class Role : Entity<RoleId>
{
    private readonly List<string> _permissions = [];

    public string Name { get; private set; } = default!;
    public string Description { get; private set; } = default!;
    public IReadOnlyList<string> Permissions => _permissions.AsReadOnly();

    // EF
    private Role() { }

    private Role(RoleId id, string name, string description, IEnumerable<string> permissions) : base(id)
    {
        Name = name;
        Description = description;
        _permissions.AddRange(permissions);
    }

    public static Role Create(RoleId id, string name, string description, IEnumerable<string> permissions) =>
        new(id, name, description, permissions);

    /// <summary>
    /// Stable role identifiers. The same values are seeded into the database via
    /// <c>infra/db/seed/identity-roles.sql</c>. Never rotate these — they are
    /// foreign-keyed by user rows and referenced from JWT claims.
    /// </summary>
    public static class SeedIds
    {
        public static readonly RoleId Guest = new(Guid.Parse("a1d4f0b0-0001-7000-8000-000000000001"));
        public static readonly RoleId Learner = new(Guid.Parse("a1d4f0b0-0001-7000-8000-000000000002"));
        public static readonly RoleId VerifiedCreator = new(Guid.Parse("a1d4f0b0-0001-7000-8000-000000000003"));
        public static readonly RoleId Moderator = new(Guid.Parse("a1d4f0b0-0001-7000-8000-000000000004"));
        public static readonly RoleId Admin = new(Guid.Parse("a1d4f0b0-0001-7000-8000-000000000005"));
    }
}
