using Lexio.Identity.Domain.Entities;
using Lexio.Identity.Domain.Primitives;

namespace Lexio.Identity.Application.Tests;

internal static class TestFactories
{
    internal static Role Learner(Guid? id = null) =>
        Role.Create(new RoleId(id ?? Guid.NewGuid()), "learner", "Default learner role", ["vocab:read", "study:write"]);

    internal const string ValidBcrypt = "$2a$12$abcdefghijklmnopqrstuvwxyz0123456789abcdefghijklmno1234";
    internal const string AltBcrypt = "$2b$10$differentSaltdifferentSaltdifferentSaltdifferent12345";
}
