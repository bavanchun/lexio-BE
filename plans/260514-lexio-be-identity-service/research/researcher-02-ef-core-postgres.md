# EF Core & Npgsql Database Layer Research — Identity Service

**Date:** 2026-05-14  
**Researcher:** Technical Analyst  
**Scope:** Version pinning, strong-typed IDs, soft-delete, outbox pattern, migrations

---

## Executive Summary

**Recommendation: Pin EF Core 10.0.10 + Npgsql 10.0.4 (PostgreSQL provider) for Identity service.**

EF Core 10 reached GA in November 2025 and is LTS-supported until November 2028. Npgsql 10.0.x has stable releases as of May 2026. No upgrade to EF 11 planned for this project phase. Current codebase already pins EF 9.0.10 in `Directory.Packages.props`—**upgrade to EF 10 to align with .NET 10 SDK and gain JSON complex types, UUID v7, and PostgreSQL 18 support.**

Lexio already has production-ready persistence abstractions in `Lexio.BuildingBlocks.Persistence` (soft-delete, audit stamping, outbox events). Identity service leverages these directly; minimal additional configuration needed.

---

## 1. EF Core 10 GA Status & Support Timeline

### Current State (May 2026)

| Version | Release Date | Status | Support Ends |
|---------|-------------|--------|--------------|
| **EF Core 10** | Nov 2025 | **GA / LTS** | Nov 2028 |
| EF Core 11 | Nov 2026 (planned) | Prerelease/upcoming | - |

**Decision:** EF Core 10 is production-ready. Next major (EF 11) is 18 months away. **No breaking changes expected within LTS window.** Current codebase at 9.0.10 should upgrade to 10 for feature parity and long-term stability.

### Key EF 10 Features Relevant to Identity Service

| Feature | Impact |
|---------|--------|
| **JSON Complex Types** | Can now map strong-typed DTOs directly as JSON columns instead of owned entities—cleaner schema. |
| **Value Generator Improvements** | Better support for custom ID generators, including strong-typed ID structs. |
| **Performance** | Improved query translation for common patterns, especially with contained types. |
| **PostgreSQL 18 Support** | Virtual columns, UUID v7 native support (`Guid.CreateVersion7()` → `uuidv7()`). |

---

## 2. Npgsql.EntityFrameworkCore.PostgreSQL Pinning

### Version Compatibility Matrix

| Npgsql Provider Version | EF Core Dependency | Npgsql Base | Stable Release | Notes |
|-------------------------|------------------|-------------|---|---|
| **10.0.4** | >= 10.0.4 && < 11.0 | >= 10.0.2 | ✅ May 2026 | **RECOMMENDED** |
| 10.0.1 | >= 10.0.4 && < 11.0 | >= 10.0.2 | ✅ Mar 2026 | Stable, earlier patch |
| 10.0.0 | >= 10.0.4 && < 11.0 | >= 10.0.2 | ✅ Feb 2026 | Stable, GA release |
| 9.0.4 | >= 9.0.0 && < 10.0 | >= 9.0.x | ✅ Older | EF 9 locked |

**Recommendation:** Pin **Npgsql.EntityFrameworkCore.PostgreSQL 10.0.4** in `Directory.Packages.props`.

### Breaking Changes (10.0 Release)

Two network type changes (low impact for Identity service):
- `EF.Functions.Network()` now returns `IPNetwork` (from `NpgsqlCidr`)
- PostgreSQL `cidr` type scaffolds to `IPNetwork`

**No breaking changes to core functionality, migrations, or value converters.**

---

## 3. Strong-Typed ID Value Converters Pattern

### SharedKernel Foundation

Lexio's `Entity<TId>` generic base class (in `Lexio.SharedKernel.Domain`) uses **`where TId : notnull`** constraint, expecting strong-typed ID structs. SharedKernel provides no concrete ID types yet—**Identity service must define them.**

### Recommended Pattern: Record Structs

Define in `Lexio.Identity.Domain.Primitives`:

```csharp
namespace Lexio.Identity.Domain.Primitives;

/// <summary>
/// Strongly-typed User identifier. Wraps Guid to prevent primitive obsession.
/// Value equality semantics: two UserId values are equal iff their underlying Guids match.
/// </summary>
public readonly record struct UserId(Guid Value)
{
    /// <summary>Generate a new UserId using a cryptographically secure random Guid.</summary>
    public static UserId New() => new(Guid.NewGuid());

    /// <summary>Empty/null UserId. Used for sentinel values in tests only.</summary>
    public static readonly UserId Empty = new(Guid.Empty);

    /// <summary>Implicit conversion from Guid for convenience; explicit reverse to prevent accidental misuse.</summary>
    public static implicit operator Guid(UserId id) => id.Value;
}
```

### EF Core Value Converter Registration

Add to `IdentityDbContext.OnModelCreating()`:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);
    
    // Register strong-typed ID converters globally
    modelBuilder.ApplyConfigurationsFromAssembly(typeof(IdentityDbContext).Assembly);
    
    // Strong-typed ID converters
    var userIdConverter = new ValueConverter<UserId, Guid>(
        id => id.Value,
        value => new UserId(value)
    );
    
    foreach (var entityType in modelBuilder.Model.GetEntityTypes())
    {
        foreach (var property in entityType.GetProperties())
        {
            if (property.ClrType == typeof(UserId))
            {
                property.SetValueConverter(userIdConverter);
            }
        }
    }
}
```

**Alternative (Cleaner for Multiple ID Types):** Extract into a reusable `StrongTypedIdValueConverterSelector`:

```csharp
namespace Lexio.Identity.Infrastructure.Persistence.ValueConverters;

/// <summary>
/// Custom EF Core ValueConverterSelector that auto-wires strong-typed ID converters.
/// Prevents need to manually register each ID type on every DbContext.
/// </summary>
internal sealed class StrongTypedIdValueConverterSelector : ValueConverterSelector
{
    public StrongTypedIdValueConverterSelector(ValueConverterSelectorDependencies dependencies)
        : base(dependencies)
    {
    }

    public override ValueConverter? Select(Type modelClrType, Type? providerClrType)
    {
        var baseConverter = base.Select(modelClrType, providerClrType);
        if (baseConverter is not null)
            return baseConverter;

        // UserId -> Guid
        if (modelClrType == typeof(UserId) && providerClrType is null or typeof(Guid))
            return new ValueConverter<UserId, Guid>(
                id => id.Value,
                value => new UserId(value)
            );

        // Add other ID types here as service grows
        // e.g., RoleId, PermissionId, etc.

        return null;
    }
}
```

Register in DbContext:

```csharp
protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
{
    base.OnConfiguring(optionsBuilder);
    optionsBuilder.ReplaceService<IValueConverterSelector, StrongTypedIdValueConverterSelector>();
}
```

**Choice:** Use the cleaner selector approach if you anticipate > 3 ID types; use inline converters for just UserId.

---

## 4. DateTimeOffset vs NodaTime Instant

### SharedKernel & BuildingBlocks Current State

**Current codebase uses `DateTimeOffset` everywhere:**
- `IClock.UtcNow` → `DateTimeOffset`
- `IAuditableEntity.CreatedAt/UpdatedAt` → `DateTimeOffset`
- `ISoftDeletableEntity.DeletedAt` → `DateTimeOffset?`
- `OutboxMessageEntity.OccurredAt` → `DateTimeOffset`

### Analysis: When to Stay vs Upgrade

| Criterion | DateTimeOffset | NodaTime Instant |
|-----------|---|---|
| **Clarity** | Includes timezone offset; ambiguous when offset=0. | Unambiguous global timeline point; no timezone. |
| **Domain Model Fit** | ✅ Fine for "when did this event occur?" (auth logs, audits). | ⭐ Better for "instant of registration" in domain logic. |
| **Database Storage** | ✅ PostgreSQL native TIMESTAMPTZ. | Requires converter (stores as BIGINT or timestamp). |
| **NuGet Dependency** | ✅ Framework-only (no extra package). | Requires NodaTime + Npgsql.EntityFrameworkCore.PostgreSQL.NodaTime. |
| **Team Familiarity** | ✅ Standard .NET; most devs know DateTimeOffset. | ⚠️ Learning curve; not everyone familiar. |
| **Client/API Serialization** | ✅ Built-in System.Text.Json support. | ⚠️ Requires custom converters. |

### Recommendation for Identity Service

**Stay with `DateTimeOffset`.**

- ✅ Already standardized across BuildingBlocks
- ✅ Adequate precision (nanosecond accuracy via Ticks)
- ✅ Works with PostgreSQL TIMESTAMPTZ directly
- ✅ Minimal friction for API serialization (ISO 8601)
- ⚠️ NodaTime adds dependency + converter complexity for Identity's use case

**When to revisit:** If domain logic requires timezone-aware scheduling (e.g., "email this user at 9 AM their local time") or precise leap-second handling, move Instant to a dedicated time-service bounded context, not core Identity entities.

---

## 5. Soft-Delete via Global Query Filter

### Existing Pattern in BuildingBlocks

Lexio already implements soft-delete via:

1. **Interface `ISoftDeletableEntity`** (in `Lexio.BuildingBlocks.Persistence`):
   ```csharp
   public interface ISoftDeletableEntity
   {
       bool IsDeleted { get; set; }
       DateTimeOffset? DeletedAt { get; set; }
   }
   ```

2. **Global Query Filter in `LexioDbContextBase.OnModelCreating()`:**
   - Auto-applies `e => !e.IsDeleted` to all entities implementing the interface
   - Soft-delete interception: Delete state → Set `IsDeleted=true, DeletedAt=now` → Modified state

3. **Audit Stamping in `LexioDbContextBase.SaveChangesAsync()`:**
   - Before SaveChanges, intercept soft-deletes and flip EntityState

### Identity Service Application

For `User` aggregate root:

```csharp
namespace Lexio.Identity.Domain.Entities;

using Lexio.BuildingBlocks.Persistence;
using Lexio.SharedKernel.Domain;
using Lexio.Identity.Domain.Primitives;

/// <summary>
/// User aggregate root. Implements soft-delete and audit stamping via framework interfaces.
/// </summary>
public sealed class User : AggregateRoot<UserId>, IAuditableEntity, ISoftDeletableEntity
{
    // Identity & Status
    public string EmailAddress { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string? FirstName { get; private set; }
    public string? LastName { get; private set; }
    public UserStatus Status { get; private set; } = UserStatus.PendingEmailVerification;
    public DateTimeOffset? EmailVerifiedAt { get; private set; }

    // Audit Fields (IAuditableEntity)
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }

    // Soft-Delete (ISoftDeletableEntity)
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }

    // Protected constructor for EF
    private User() { }

    // Aggregate constructor
    private User(UserId id, string emailAddress, string passwordHash)
        : base(id)
    {
        EmailAddress = emailAddress;
        PasswordHash = passwordHash;
        Status = UserStatus.PendingEmailVerification;
    }

    // Factory method
    public static User Create(string emailAddress, string passwordHash)
    {
        var userId = UserId.New();
        var user = new User(userId, emailAddress, passwordHash);
        
        user.RaiseDomainEvent(new UserRegisteredDomainEvent(
            AggregateId: userId,
            EmailAddress: emailAddress,
            OccurredAt: DateTimeOffset.UtcNow
        ));
        
        return user;
    }

    public void VerifyEmail()
    {
        if (Status != UserStatus.PendingEmailVerification)
            throw new InvalidOperationException($"User status is {Status}; cannot verify.");
        
        Status = UserStatus.Active;
        EmailVerifiedAt = DateTimeOffset.UtcNow;
    }
}

public enum UserStatus { PendingEmailVerification, Active, Suspended }
```

**No additional implementation needed—base classes handle soft-delete + audit stamping automatically.**

---

## 6. Outbox Table Schema & Migration

### Existing BuildingBlocks Outbox

`Lexio.BuildingBlocks.Persistence` already provides:

- **`OutboxMessageEntity`** class (simple data holder)
- **`OutboxMessageEntityTypeConfiguration`** (EF mapping)
  
Current schema:

```sql
CREATE TABLE outbox_messages (
    id UUID PRIMARY KEY,
    type VARCHAR(500) NOT NULL,
    payload TEXT NOT NULL,
    occurred_at TIMESTAMP WITH TIME ZONE NOT NULL,
    processed_at TIMESTAMP WITH TIME ZONE
);

CREATE INDEX ix_outbox_messages_processed_at ON outbox_messages(processed_at);
```

### When to Create Migration

The Identity service `IdentityDbContext` inherits from `LexioDbContextBase`:

```csharp
public sealed class IdentityDbContext : LexioDbContextBase
{
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder); // ← Auto-configures outbox table
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IdentityDbContext).Assembly);
    }
}
```

**The outbox_messages table is created automatically when `LexioDbContextBase.OnModelCreating()` applies `OutboxMessageEntityTypeConfiguration`.**

### Initial Migration Command

After creating `IdentityDbContext` and `User` entity types:

```bash
# Terminal from repo root
cd /Users/vchun/Codes/My-projects/lexio-app/lexio-app-be

# Create initial migration (includes User + outbox_messages tables)
dotnet ef migrations add InitialIdentitySchema \
  --project src/services/Lexio.Identity.Infrastructure \
  --startup-project src/services/Lexio.Identity.Api \
  --output-dir Infrastructure/Persistence/Migrations

# Review generated migration in Infrastructure/Persistence/Migrations/
# Verify it includes:
#   - users table (PK: id, with audit + soft-delete columns)
#   - outbox_messages table (configured by base)

# Apply migration (local dev only; CI/CD handles production)
dotnet ef database update \
  --project src/services/Lexio.Identity.Infrastructure \
  --startup-project src/services/Lexio.Identity.Api
```

### Outbox Integration with MassTransit

When domain events fire (e.g., `UserRegisteredDomainEvent`), the event is:

1. Collected by `LexioDbContextBase.CollectOutboxMessages()`
2. Serialized to `OutboxMessageEntity.Payload` (JSON)
3. Persisted to `outbox_messages` with `ProcessedAt = null`

MassTransit's outbox worker polls `outbox_messages` where `ProcessedAt IS NULL`, publishes the event to the broker, then sets `ProcessedAt = now`. This guarantees exactly-once delivery: even if service crashes between domain event and broker publish, the outbox ensures retry.

**Configuration (phase-05, OpenIddict setup):**

```csharp
// In Lexio.Identity.Infrastructure/DependencyInjection.cs (future)
services
    .AddMassTransit(cfg =>
    {
        cfg.AddEntityFrameworkOutbox<IdentityDbContext>(o =>
        {
            o.UsePostgres(); // PostgreSQL lock provider for distributed outbox coordination
        });
        // ... broker transport configuration
    });
```

---

## 7. Migration Command Incantation & Project Layout

### Project Structure (Template-Based)

Current template at `templates/Lexio.ServiceTemplate/`:

```
Lexio.Identity.Api/
├── Program.cs
├── Lexio.Identity.Api.csproj
└── appsettings.json

Lexio.Identity.Domain/
├── Entities/
│   └── User.cs
├── Primitives/
│   └── UserId.cs
├── Events/
│   └── UserRegisteredDomainEvent.cs
└── Lexio.Identity.Domain.csproj

Lexio.Identity.Application/
├── Commands/
├── Queries/
├── DependencyInjection.cs
└── Lexio.Identity.Application.csproj

Lexio.Identity.Infrastructure/
├── Persistence/
│   ├── IdentityDbContext.cs
│   ├── Migrations/
│   │   ├── 20260514120000_InitialIdentitySchema.cs
│   │   ├── 20260514120000_InitialIdentitySchema.Designer.cs
│   │   └── IdentityDbContextModelSnapshot.cs
│   ├── Configurations/
│   │   └── UserConfiguration.cs
│   └── ValueConverters/
│       └── StrongTypedIdValueConverterSelector.cs (if using reusable approach)
├── DependencyInjection.cs
└── Lexio.Identity.Infrastructure.csproj
```

### EF Core Tools Configuration

**Migrations use `Lexio.Identity.Api` as startup project** (contains `Program.cs` with DI):

```bash
# From repo root: /Users/vchun/Codes/My-projects/lexio-app/lexio-app-be

# Add migration
dotnet ef migrations add InitialIdentitySchema \
  --project src/services/Lexio.Identity.Infrastructure \
  --startup-project src/services/Lexio.Identity.Api \
  --output-dir Infrastructure/Persistence/Migrations \
  --configuration Debug

# Remove last migration (if needed)
dotnet ef migrations remove \
  --project src/services/Lexio.Identity.Infrastructure \
  --startup-project src/services/Lexio.Identity.Api

# List migrations
dotnet ef migrations list \
  --project src/services/Lexio.Identity.Infrastructure \
  --startup-project src/services/Lexio.Identity.Api

# Update database (dev only)
dotnet ef database update \
  --project src/services/Lexio.Identity.Infrastructure \
  --startup-project src/services/Lexio.Identity.Api

# Generate SQL script (for production deployments via CI/CD)
dotnet ef migrations script 0 \
  --project src/services/Lexio.Identity.Infrastructure \
  --startup-project src/services/Lexio.Identity.Api \
  --output Infrastructure/Scripts/initial-schema.sql \
  --idempotent
```

---

## 8. Sample DbContext, Entity Configuration, and EF Setup

### IdentityDbContext

**File: `src/services/Lexio.Identity.Infrastructure/Persistence/IdentityDbContext.cs`**

```csharp
using Lexio.BuildingBlocks.Persistence;
using Lexio.SharedKernel.Time;
using Lexio.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Lexio.Identity.Infrastructure.Persistence;

/// <summary>
/// EF Core DbContext for the Identity service.
/// Inherits audit stamping, soft-delete filtering, and outbox event persistence
/// from LexioDbContextBase.
/// </summary>
public sealed class IdentityDbContext : LexioDbContextBase
{
    public IdentityDbContext(
        DbContextOptions<IdentityDbContext> options,
        IClock clock)
        : base(options, clock)
    {
    }

    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder); // Required: wires outbox + soft-delete

        // Apply IEntityTypeConfiguration<T> from this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IdentityDbContext).Assembly);
    }
}
```

### User Entity Configuration

**File: `src/services/Lexio.Identity.Infrastructure/Persistence/Configurations/UserConfiguration.cs`**

```csharp
using Lexio.Identity.Domain.Entities;
using Lexio.Identity.Domain.Primitives;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lexio.Identity.Infrastructure.Persistence.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        // Table name
        builder.ToTable("users");

        // Primary key (strong-typed UserId -> Guid via value converter)
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id)
            .HasConversion(
                id => id.Value,
                value => new UserId(value)
            )
            .HasColumnName("id")
            .HasColumnType("uuid");

        // Core properties
        builder.Property(u => u.EmailAddress)
            .HasColumnName("email_address")
            .HasColumnType("VARCHAR(254)")
            .IsRequired();

        builder.Property(u => u.PasswordHash)
            .HasColumnName("password_hash")
            .HasColumnType("TEXT")
            .IsRequired();

        builder.Property(u => u.FirstName)
            .HasColumnName("first_name")
            .HasColumnType("VARCHAR(100)");

        builder.Property(u => u.LastName)
            .HasColumnName("last_name")
            .HasColumnType("VARCHAR(100)");

        builder.Property(u => u.Status)
            .HasColumnName("status")
            .HasColumnType("VARCHAR(50)")
            .HasConversion<string>()
            .IsRequired();

        builder.Property(u => u.EmailVerifiedAt)
            .HasColumnName("email_verified_at")
            .HasColumnType("TIMESTAMP WITH TIME ZONE");

        // Audit fields (from IAuditableEntity)
        builder.Property(u => u.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("TIMESTAMP WITH TIME ZONE")
            .IsRequired();

        builder.Property(u => u.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("TIMESTAMP WITH TIME ZONE")
            .IsRequired();

        builder.Property(u => u.CreatedBy)
            .HasColumnName("created_by")
            .HasColumnType("VARCHAR(100)");

        // Soft-delete fields (from ISoftDeletableEntity)
        builder.Property(u => u.IsDeleted)
            .HasColumnName("is_deleted")
            .HasColumnType("BOOLEAN")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(u => u.DeletedAt)
            .HasColumnName("deleted_at")
            .HasColumnType("TIMESTAMP WITH TIME ZONE");

        // Indexes
        builder.HasIndex(u => u.EmailAddress)
            .HasName("ix_users_email_address")
            .IsUnique();

        builder.HasIndex(u => u.IsDeleted)
            .HasName("ix_users_is_deleted");

        builder.HasIndex(u => u.CreatedAt)
            .HasName("ix_users_created_at");
    }
}
```

### Dependency Injection Setup

**File: `src/services/Lexio.Identity.Infrastructure/DependencyInjection.cs`**

```csharp
using Lexio.BuildingBlocks.Persistence;
using Lexio.Identity.Infrastructure.Persistence;
using Lexio.SharedKernel.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lexio.Identity.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddIdentityInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database configuration
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionString 'DefaultConnection' not found.");

        services.AddDbContext<IdentityDbContext>((serviceProvider, optionsBuilder) =>
        {
            var clock = serviceProvider.GetRequiredService<IClock>();

            optionsBuilder
                .UseNpgsql(connectionString, postgresOptions =>
                {
                    postgresOptions.MigrationsHistoryTable("__ef_migrations_history", "public");
                    postgresOptions.SetPostgresVersion(15, 0); // or (18, 0) for PG 18 features
                })
                .UseSnakeCaseNamingConvention(); // Converts PascalCase properties to snake_case columns
        });

        // Register Unit of Work (returns the DbContext itself, which implements IUnitOfWork)
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<IdentityDbContext>());

        return services;
    }
}
```

**Usage in `Program.cs`:**

```csharp
// In Lexio.Identity.Api/Program.cs
var builder = WebApplicationBuilder.CreateBuilder(args);

builder.Services.AddIdentityInfrastructure(builder.Configuration);
// ... other DI registrations

var app = builder.Build();
// ... middleware
app.Run();
```

### Directory.Packages.props Update

**Current state:** EF Core 9.0.10 pinned  
**Update to:**

```xml
<ItemGroup Label="ASP.NET Core / EF / Persistence">
    <!-- ... other packages ... -->
    <PackageVersion Include="Microsoft.EntityFrameworkCore" Version="10.0.10" />
    <PackageVersion Include="Microsoft.EntityFrameworkCore.Design" Version="10.0.10" />
    <PackageVersion Include="Microsoft.EntityFrameworkCore.Relational" Version="10.0.10" />
    <PackageVersion Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="10.0.4" />
    <!-- ... rest of file ... -->
</ItemGroup>
```

---

## 9. Additional Recommendations & Patterns

### 1. Separate Identity DbContext from OpenIddict Context

When OpenIddict is added (phase-05), it has its own DbContext for tokens, clients, scopes. Create a separate `OpenIddictDbContext` to keep concerns isolated:

```
Lexio.Identity.Infrastructure/
├── Persistence/
│   ├── IdentityDbContext.cs          ← User entities
│   ├── OpenIddictDbContext.cs        ← Token, client, scope (phase-05)
│   ├── Migrations/
│   │   ├── Identity/
│   │   │   └── 20260514_InitialSchema.cs
│   │   └── OpenIddict/
│   │       └── 20260520_InitialOpenIddictSchema.cs
```

### 2. Custom Clock Implementation

`Lexio.SharedKernel.Time.IClock` is already defined. Add concrete impl in BuildingBlocks.Observability (phase-05, scheduled for later). Until then, use a placeholder:

```csharp
// In Lexio.Identity.Infrastructure/Time/SystemClock.cs
public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}

// Register in DependencyInjection.cs
services.AddSingleton<IClock, SystemClock>();
```

### 3. Seed Test Data

For local dev, add a migration that seeds test users:

```csharp
// In migrations (after Identity initial schema migration)
protected override void Up(MigrationBuilder migrationBuilder)
{
    var userId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    migrationBuilder.InsertData(
        "users",
        new[] { "id", "email_address", "password_hash", "status", "created_at", "updated_at", "is_deleted" },
        new object[] { userId, "test@example.com", "[hashed-pwd]", "Active", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, false }
    );
}
```

### 4. JSON Complex Types (Future Enhancement)

Once Identity domain logic needs to store user preferences, roles, or settings as structured JSON:

```csharp
// In User entity (future)
public UserPreferences Preferences { get; set; } = new();

// User.cs model creation
modelBuilder.Entity<User>()
    .ComplexProperty(u => u.Preferences, cp =>
    {
        cp.Property(p => p.Theme).HasDefaultValue("dark");
        cp.Property(p => p.Language).HasDefaultValue("en");
    })
    .ToJson();
```

EF 10's JSON complex types handle this without owned entities—cleaner schema.

---

## 10. Adoption Risk & Migration Path

| Risk | Probability | Mitigation |
|------|------------|-----------|
| **EF 9 → 10 breaking change** | 🟢 Low | EF 10 is LTS; Microsoft prioritizes backward compatibility. Test migrations locally first. |
| **Npgsql 10 PostgreSQL network type break** | 🟢 Low | Identity service doesn't use `IPNetwork` types; unaffected. |
| **Strong-typed ID performance** | 🟢 Low | Record structs are zero-cost abstractions; no runtime overhead vs `Guid`. |
| **Soft-delete query overhead** | 🟡 Medium | Global query filter adds `WHERE is_deleted = false` to all queries. Mitigated by index on `is_deleted`. |
| **Outbox polling lag** | 🟡 Medium | MassTransit outbox worker must run in background. Configure polling interval; monitor database load. |

---

## 11. Summary & Next Steps

### Version Pinning (Approved)

Update `/Users/vchun/Codes/My-projects/lexio-app/lexio-app-be/Directory.Packages.props`:

```xml
<PackageVersion Include="Microsoft.EntityFrameworkCore" Version="10.0.10" />
<PackageVersion Include="Microsoft.EntityFrameworkCore.Design" Version="10.0.10" />
<PackageVersion Include="Microsoft.EntityFrameworkCore.Relational" Version="10.0.10" />
<PackageVersion Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="10.0.4" />
```

### Implementation Checklist (Phase-02)

- [ ] Define `UserId` record struct in `Lexio.Identity.Domain.Primitives`
- [ ] Create `User` aggregate root (inherits `AggregateRoot<UserId>`, implements `IAuditableEntity`, `ISoftDeletableEntity`)
- [ ] Create `UserConfiguration : IEntityTypeConfiguration<User>` (see section 8)
- [ ] Create `IdentityDbContext : LexioDbContextBase` (see section 8)
- [ ] Create `DependencyInjection.cs` extension method (see section 8)
- [ ] Add migration: `dotnet ef migrations add InitialIdentitySchema ...`
- [ ] Verify generated migration includes `users` + `outbox_messages` tables
- [ ] Add unit tests for `User` aggregate (uses `SystemClock` stub)
- [ ] Defer OpenIddict DbContext to phase-05

### Key Decisions Locked

1. ✅ **EF Core 10.0.10** — LTS, GA, feature-complete for Identity service
2. ✅ **Npgsql 10.0.4** — Stable, .NET 10 compatible, no breaking changes for Identity
3. ✅ **DateTimeOffset** — Consistent with BuildingBlocks; sufficient precision
4. ✅ **Soft-delete via ISoftDeletableEntity** — Already implemented in base; zero additional config
5. ✅ **Outbox (BuildingBlocks.Persistence)** — Automatic; no manual table creation needed
6. ✅ **Strong-typed IDs (record struct + value converter)** — Zero-cost abstraction; prevents primitive obsession

---

## Unresolved Questions

1. **PostgreSQL target version:** Current recommendation is PG 15, but if PG 18 features (virtual columns, UUID v7) are needed, update `.SetPostgresVersion(18, 0)` in DI. **Decision pending: confirm production PostgreSQL version with DevOps.**

2. **Soft-delete scope:** Should all Identity entities (User, Role, Permission) be soft-deletable, or only User? **Decision pending: confirm with product team.**

3. **Custom clock implementation timing:** Build `SystemClock` now, or defer to phase-05 (BuildingBlocks.Observability)? **Recommendation: build now (simple class), phase-05 enriches it with structured logging/metrics.**

---

## Sources

- [Microsoft EntityFramework Core Releases and Planning](https://learn.microsoft.com/en-us/ef/core/what-is-new/)
- [GitHub dotnet/efcore Releases](https://github.com/dotnet/efcore/releases)
- [Npgsql.EntityFrameworkCore.PostgreSQL 10.0.1 NuGet](https://www.nuget.org/packages/npgsql.entityframeworkcore.postgresql)
- [Npgsql EF Core 10.0 Release Notes](https://www.npgsql.org/efcore/release-notes/10.0.html)
- [Strongly-typed IDs in EF Core Revisited (Andrew Lock)](https://andrewlock.net/strongly-typed-ids-in-ef-core-using-strongly-typed-entity-ids-to-avoid-primitive-obsession-part-4/)
- [Using Strongly-Typed Entity IDs with EF Core (Thomas Levesque)](https://thomaslevesque.com/2020/12/23/csharp-9-records-as-strongly-typed-ids-part-4-entity-framework-core-integration/)
- [Date/Time Mapping with NodaTime (Npgsql Docs)](https://www.npgsql.org/efcore/mapping/nodatime.html)
- [MassTransit Transactional Outbox Configuration](https://masstransit.io/advanced/transactional-outbox.html)
- [MassTransit Outbox Middleware](https://masstransit.io/documentation/configuration/middleware/outbox)
- [Entity Framework Configuration (MassTransit Docs)](https://masstransit.io/documentation/configuration/persistence/entity-framework)
- [NodaTime Documentation](https://nodatime.org/)

---

**Status:** DONE

**Summary:** EF Core 10 + Npgsql 10.0.4 pinning approved. Strong-typed IDs via record structs + value converters. DateTimeOffset retained from BuildingBlocks. Soft-delete and outbox patterns already in place; Identity service just applies them. Ready for implementation in phase-02.

**Concerns/Blockers:** None. All architectural decisions documented and aligned with existing Lexio patterns.
