using Lexio.Identity.Domain.Entities;
using Lexio.Identity.Domain.ValueObjects;
using Lexio.Identity.Infrastructure.Persistence.ValueConverters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lexio.Identity.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.ToTable("users", t =>
            // Enforce normalised email at the storage layer so direct-SQL writers can't bypass the Email VO.
            t.HasCheckConstraint("ck_users_email_lowercase", "email = lower(email)"));

        b.HasKey(u => u.Id);
        b.Property(u => u.Id)
            .HasConversion<UserIdConverter>()
            .HasDefaultValueSql("uuidv7()");

        b.Property(u => u.Email)
            .HasConversion(e => e.Value, raw => Email.From(raw))
            .HasColumnName("email")
            .HasMaxLength(Email.MaxLength)
            .IsRequired();

        b.Property(u => u.PasswordHash)
            .HasConversion(p => p == null ? null : p.Value, raw => raw == null ? null : PasswordHash.From(raw))
            .HasColumnName("password_hash")
            .HasColumnType("text");

        b.Property(u => u.DisplayName)
            .HasConversion(d => d.Value, raw => DisplayName.From(raw))
            .HasColumnName("display_name")
            .HasMaxLength(DisplayName.MaxLength)
            .IsRequired();

        b.Property(u => u.RoleId)
            .HasConversion<RoleIdConverter>()
            .IsRequired();

        b.Property(u => u.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        b.Property(u => u.IsVerified).IsRequired();
        b.Property(u => u.EmailVerifiedAt);
        b.Property(u => u.LastLoginAt);
        b.Property(u => u.BannedReason).HasMaxLength(500);
        b.Property(u => u.BannedAt);

        b.Property(u => u.CreatedAt).IsRequired();
        b.Property(u => u.UpdatedAt).IsRequired();
        b.Property(u => u.CreatedBy).HasMaxLength(64);

        b.Property(u => u.IsDeleted).IsRequired();
        b.Property(u => u.DeletedAt);

        b.HasIndex(u => u.Email).HasDatabaseName("ix_users_email_unique").IsUnique();
        b.HasIndex(u => u.RoleId).HasDatabaseName("ix_users_role_id");

        // Typed FK to Role keeps users.role_id referentially integral. Restrict: cannot
        // remove a role while any user references it (seed roles never get deleted).
        b.HasOne<Role>()
            .WithMany()
            .HasForeignKey(u => u.RoleId)
            .OnDelete(DeleteBehavior.Restrict);

        // Typed FKs to the owned-but-not-owned child collections. Specifying the property
        // explicitly prevents EF from synthesising a shadow `user_id1` column.
        b.HasMany(u => u.RefreshTokens)
            .WithOne()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasMany(u => u.OAuthConnections)
            .WithOne()
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
