using System.Text.Json;
using Lexio.Identity.Domain.Entities;
using Lexio.Identity.Infrastructure.Persistence.ValueConverters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lexio.Identity.Infrastructure.Persistence.Configurations;

public sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public void Configure(EntityTypeBuilder<Role> b)
    {
        b.ToTable("roles");

        b.HasKey(r => r.Id);
        b.Property(r => r.Id)
            .HasConversion<RoleIdConverter>()
            .HasDefaultValueSql("uuidv7()");

        b.Property(r => r.Name).HasMaxLength(64).IsRequired();
        b.Property(r => r.Description).HasMaxLength(500).IsRequired();
        b.HasIndex(r => r.Name).IsUnique();

        var permComparer = new ValueComparer<IReadOnlyList<string>>(
            (a, c) => a!.SequenceEqual(c!),
            v => v.Aggregate(0, (h, s) => HashCode.Combine(h, s.GetHashCode())),
            v => v.ToList());

        b.Property(r => r.Permissions)
            .HasConversion(
                v => JsonSerializer.Serialize(v, Json),
                v => JsonSerializer.Deserialize<List<string>>(v, Json) ?? new List<string>())
            .HasColumnType("jsonb")
            .HasField("_permissions")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .Metadata.SetValueComparer(permComparer);
    }
}
