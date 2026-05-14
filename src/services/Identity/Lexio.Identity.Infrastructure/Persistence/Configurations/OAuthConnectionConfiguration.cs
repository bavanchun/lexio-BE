using Lexio.Identity.Domain.Entities;
using Lexio.Identity.Infrastructure.Persistence.ValueConverters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lexio.Identity.Infrastructure.Persistence.Configurations;

public sealed class OAuthConnectionConfiguration : IEntityTypeConfiguration<OAuthConnection>
{
    public void Configure(EntityTypeBuilder<OAuthConnection> b)
    {
        b.ToTable("oauth_connections");

        b.HasKey(c => c.Id);
        b.Property(c => c.Id)
            .HasConversion<OAuthConnectionIdConverter>()
            .HasDefaultValueSql("uuidv7()");

        b.Property(c => c.UserId).HasConversion<UserIdConverter>().IsRequired();
        b.Property(c => c.Provider).HasConversion<string>().HasMaxLength(32).IsRequired();
        b.Property(c => c.ProviderUserId).HasMaxLength(128).IsRequired();
        b.Property(c => c.ConnectedAt).IsRequired();
        b.Property(c => c.LastUsedAt);

        b.HasIndex(c => new { c.Provider, c.ProviderUserId }).IsUnique();
    }
}
