using Lexio.Identity.Domain.Entities;
using Lexio.Identity.Infrastructure.Persistence.ValueConverters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lexio.Identity.Infrastructure.Persistence.Configurations;

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> b)
    {
        b.ToTable("refresh_tokens");

        b.HasKey(t => t.Id);
        b.Property(t => t.Id)
            .HasConversion<RefreshTokenIdConverter>()
            .HasDefaultValueSql("uuidv7()");

        b.Property(t => t.UserId).HasConversion<UserIdConverter>().IsRequired();
        b.Property(t => t.TokenHash).HasMaxLength(128).IsRequired();
        b.Property(t => t.IssuedAt).IsRequired();
        b.Property(t => t.ExpiresAt).IsRequired();
        b.Property(t => t.RevokedAt);
        b.Property(t => t.IpAddress).HasMaxLength(64);

        b.HasIndex(t => t.TokenHash).IsUnique();
        b.HasIndex(nameof(RefreshToken.UserId), nameof(RefreshToken.RevokedAt))
            .HasDatabaseName("ix_refresh_tokens_user_id_revoked_at");
    }
}
