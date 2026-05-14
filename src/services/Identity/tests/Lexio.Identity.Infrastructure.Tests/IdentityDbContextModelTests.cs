using Lexio.Identity.Domain.Entities;
using Lexio.Identity.Infrastructure.Persistence;
using Lexio.SharedKernel.Time;
using Microsoft.EntityFrameworkCore;

namespace Lexio.Identity.Infrastructure.Tests;

public class IdentityDbContextModelTests
{
    private static IdentityDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseNpgsql("Host=localhost;Database=x;Username=x;Password=x",
                npg => npg.SetPostgresVersion(18, 0))
            .UseSnakeCaseNamingConvention()
            .Options;
        return new IdentityDbContext(options, new FixedClock());
    }

    [Theory]
    [InlineData(nameof(User), "users")]
    [InlineData(nameof(Role), "roles")]
    [InlineData(nameof(RefreshToken), "refresh_tokens")]
    [InlineData(nameof(OAuthConnection), "oauth_connections")]
    public void Entity_maps_to_expected_snake_case_table(string clrName, string tableName)
    {
        using var ctx = NewContext();
        var entityType = ctx.Model.GetEntityTypes().Single(e => e.ClrType.Name == clrName);
        entityType.GetTableName().Should().Be(tableName);
    }

    [Fact]
    public void User_email_column_is_unique()
    {
        using var ctx = NewContext();
        var et = ctx.Model.FindEntityType(typeof(User))!;
        et.GetIndexes().Should().Contain(ix =>
            ix.IsUnique
            && ix.Properties.Count == 1
            && ix.Properties[0].Name == nameof(User.Email));
    }

    [Fact]
    public void RefreshToken_token_hash_is_unique()
    {
        using var ctx = NewContext();
        var et = ctx.Model.FindEntityType(typeof(RefreshToken))!;
        et.GetIndexes().Should().Contain(ix =>
            ix.IsUnique
            && ix.Properties.Count == 1
            && ix.Properties[0].Name == nameof(RefreshToken.TokenHash));
    }

    [Fact]
    public void OAuthConnection_provider_and_provider_user_id_unique_composite()
    {
        using var ctx = NewContext();
        var et = ctx.Model.FindEntityType(typeof(OAuthConnection))!;
        et.GetIndexes().Should().Contain(ix =>
            ix.IsUnique
            && ix.Properties.Count == 2
            && ix.Properties.Any(p => p.Name == nameof(OAuthConnection.Provider))
            && ix.Properties.Any(p => p.Name == nameof(OAuthConnection.ProviderUserId)));
    }

    [Theory]
    [InlineData(typeof(User))]
    [InlineData(typeof(Role))]
    [InlineData(typeof(RefreshToken))]
    [InlineData(typeof(OAuthConnection))]
    public void Primary_key_default_is_uuidv7(Type clrType)
    {
        using var ctx = NewContext();
        var et = ctx.Model.FindEntityType(clrType)!;
        var pk = et.FindPrimaryKey()!;
        pk.Properties.Single().GetDefaultValueSql().Should().Be("uuidv7()");
    }

    [Theory]
    [InlineData(typeof(RefreshToken), nameof(RefreshToken.UserId))]
    [InlineData(typeof(OAuthConnection), nameof(OAuthConnection.UserId))]
    public void Child_table_has_exactly_one_user_fk_on_typed_property(Type clrType, string fkPropertyName)
    {
        using var ctx = NewContext();
        var et = ctx.Model.FindEntityType(clrType)!;
        var fks = et.GetForeignKeys().Where(fk => fk.PrincipalEntityType.ClrType == typeof(User)).ToList();
        fks.Should().ContainSingle("there must be exactly one FK to User, with no shadow column");
        fks[0].Properties.Single().Name.Should().Be(fkPropertyName);
    }

    [Fact]
    public void User_has_fk_to_roles_role_id()
    {
        using var ctx = NewContext();
        var et = ctx.Model.FindEntityType(typeof(User))!;
        var fks = et.GetForeignKeys().Where(fk => fk.PrincipalEntityType.ClrType == typeof(Role)).ToList();
        fks.Should().ContainSingle();
        fks[0].Properties.Single().Name.Should().Be(nameof(User.RoleId));
        fks[0].DeleteBehavior.Should().Be(Microsoft.EntityFrameworkCore.DeleteBehavior.Restrict);
    }

    [Fact]
    public void EmbeddedResource_identity_roles_sql_is_packaged()
    {
        // Ensures the migration's seed read path stays wired post-publish.
        var asm = typeof(IdentityDbContext).Assembly;
        var names = asm.GetManifestResourceNames();
        names.Should().Contain(n => n.EndsWith("identity-roles.sql", StringComparison.Ordinal));
    }

    private sealed class FixedClock : IClock
    {
        public DateTimeOffset UtcNow => new(2026, 5, 14, 0, 0, 0, TimeSpan.Zero);
    }
}
