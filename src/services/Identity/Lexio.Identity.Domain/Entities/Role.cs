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
}
