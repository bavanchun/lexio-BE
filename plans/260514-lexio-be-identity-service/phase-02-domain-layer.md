# Phase 02 — Domain layer (aggregates, value objects, events)

## Context Links
- researcher-04 §2 (entity schemas), §6 (domain events)
- researcher-02 §3 (strong-typed IDs), §5 (soft-delete + audit interfaces)
- `src/shared/Lexio.SharedKernel/Domain/` (`AggregateRoot<TId>`, `Entity<TId>`, domain-event base)
- `src/shared/Lexio.BuildingBlocks.Persistence/` (`IAuditableEntity`, `ISoftDeletableEntity`)

## Overview
- Priority: P1
- Status: pending
- Effort: 4h
- Branch: `feat/be-identity-domain` (off `feat/be-identity-scaffold`)
- PR: stacked PR #14

Pure-C# domain model. Zero infrastructure dependencies. Aggregate root `User` + supporting entities `Role`, `RefreshToken`, `OAuthConnection`. Value objects `UserId`, `Email`, `PasswordHash`, `DisplayName`. Five domain events.

## Key Insights
- `Email` value object normalises to lowercase + trims; equality is case-insensitive — DB unique index must be on lowered column or expression index.
- `PasswordHash` wraps an already-hashed bcrypt string; constructor refuses anything that doesn't start with `$2a$` / `$2b$` / `$2y$` to prevent accidental plaintext storage.
- `RefreshToken` stores only the bcrypt-hashed token; raw token never leaves the application service that issued it.
- Domain events raised inside aggregate methods (factory + state transitions); `LexioDbContextBase` collects them into the outbox during SaveChanges.

## Requirements
**Functional**
- `User.Register(email, passwordHash, displayName)` factory creates User in `PendingEmailVerification` status (or `Active` since MVP skips verification — locked per researcher-04 §9.2; defer flag stored as field `isVerified=false`, status=`Active`).
- `User.VerifyEmail()` transitions to `Active`+sets `EmailVerifiedAt` (kept for Phase 1.5).
- `User.ChangePassword(newHash)` raises `PasswordChangedDomainEvent`.
- `User.ChangeRole(newRoleId, byAdminId)` raises `RoleChangedDomainEvent` with old+new.
- `User.Ban(reason, byAdminId)` sets status=`Banned`, raises `UserBannedDomainEvent`.
- `User.IssueRefreshToken(tokenHash, expiresAt, ip)` returns `RefreshToken` entity bound to user.
- `User.RevokeRefreshToken(tokenId)` sets `revokedAt` on matching token (no-op if already revoked — idempotent).

**Non-functional**
- All mutating methods enforce invariants via guard clauses (throws `DomainException` from `Lexio.SharedKernel.Domain`).
- 100% pure: no `DateTimeOffset.UtcNow` direct calls — accept `IClock` via parameter or inject at aggregate-method boundary.

## Architecture
```
Lexio.Identity.Domain/
├── Primitives/
│   ├── UserId.cs               (readonly record struct, wraps Guid)
│   ├── RoleId.cs
│   ├── RefreshTokenId.cs
│   └── OAuthConnectionId.cs
├── ValueObjects/
│   ├── Email.cs                (normalised, validated, equality)
│   ├── PasswordHash.cs         (bcrypt-prefix invariant)
│   └── DisplayName.cs          (1–100 chars)
├── Entities/
│   ├── User.cs                 (AggregateRoot<UserId>)
│   ├── Role.cs                 (Entity<RoleId>, permissions JSONB)
│   ├── RefreshToken.cs         (Entity<RefreshTokenId>)
│   └── OAuthConnection.cs      (Entity<OAuthConnectionId>)
├── Enums/
│   ├── UserStatus.cs           (Active, Banned, Deleted)
│   └── OAuthProvider.cs        (Google)
├── Events/
│   ├── UserRegisteredDomainEvent.cs
│   ├── UserLoggedInDomainEvent.cs
│   ├── PasswordChangedDomainEvent.cs
│   ├── RoleChangedDomainEvent.cs
│   └── UserBannedDomainEvent.cs
└── Exceptions/
    ├── InvalidEmailException.cs
    ├── InvalidDisplayNameException.cs
    ├── InvalidPasswordHashException.cs
    └── UserAlreadyBannedException.cs
```

## Related Code Files
**Create:** all files under `Lexio.Identity.Domain/` (~16 files).
**Modify:** none in other projects.
**Delete:** none.

## Implementation Steps
1. Define `UserId`/`RoleId`/`RefreshTokenId`/`OAuthConnectionId` record structs with `New()`, `Empty`, implicit `Guid` op.
2. Implement `Email`: factory `Email.Create(string)`, lowercases + trims + RFC 5321 regex; returns `Result<Email>`.
3. Implement `PasswordHash`: factory rejects strings not starting with `$2[aby]$`.
4. Implement `DisplayName`: 1–100 chars, alphanum + spaces/hyphens/underscores, trim-validates.
5. Implement `Role` entity: `Id`, `Name`, `Description`, `Permissions: IReadOnlyList<string>`. Static factory `Role.Seed(...)` returns 5 well-known roles.
6. Implement `RefreshToken` entity: fields per researcher-04 §2.2; `Revoke(clock)` method; `IsActive(clock)` query.
7. Implement `OAuthConnection` entity: per researcher-04 §2.4 — schema only, no behaviour in MVP.
8. Implement `User` aggregate:
   - Constructor private + EF parameterless ctor.
   - `Register(...)` factory raises `UserRegisteredDomainEvent`.
   - `RecordLogin(ipAddress, clock)` updates `LastLoginAt`, raises `UserLoggedInDomainEvent`.
   - `ChangePassword(newHash, clock)` swaps hash, raises event.
   - `ChangeRole(newRoleId, byAdminId, clock)` validates not banned, raises event.
   - `Ban(reason, byAdminId, clock)` throws `UserAlreadyBannedException` if already banned.
   - `IssueRefreshToken(...)`, `RevokeRefreshToken(...)`, `RevokeAllRefreshTokens(clock)`.
9. Wire `IAuditableEntity` + `ISoftDeletableEntity` on `User` (CreatedAt, UpdatedAt, IsDeleted, DeletedAt).
10. Write Domain.Tests covering each invariant + event emission.
11. `dotnet build /warnaserror` clean.
12. Commit + stacked PR #14.

## Todo List
- [ ] 4 strong-typed ID record structs
- [ ] 3 value objects (Email, PasswordHash, DisplayName) with `Result<T>` factories
- [ ] 4 entities (User, Role, RefreshToken, OAuthConnection)
- [ ] 5 domain events
- [ ] Domain exceptions
- [ ] Domain.Tests ≥90% coverage
- [ ] `dotnet build` warnaserror clean
- [ ] PR #14 opened, stacked on phase-01 branch

## Success Criteria
- All Domain.Tests green (≥30 tests).
- NetArchTest rule (phase-08): no `Microsoft.AspNetCore.*` or `Microsoft.EntityFrameworkCore.*` reference reachable from Domain.
- `User.Register(...)` raises exactly one `UserRegisteredDomainEvent` with correct payload.

## Risk Assessment
| Risk | L | I | Mitigation |
|---|---|---|---|
| Email canonicalisation diverges between domain + DB index | M | M | DB unique index built on `LOWER(email)`; Email VO lowers in `Create`. Integration test covers both paths. |
| Forgetting EF parameterless ctor breaks materialisation | M | M | Add unit test that uses reflection to confirm parameterless ctor exists. |
| Soft-delete + RefreshToken cascade ambiguity | L | M | RefreshTokens are NOT soft-deleted; phase-04 config sets `OnDelete(Cascade)` to hard-delete on user purge. |

## Security Considerations
- `PasswordHash` invariant blocks accidental plaintext at the type level.
- Banning is one-way (no `Unban` method in MVP — reserved Phase 2 admin tooling).
- All domain events strip PII: `UserRegisteredDomainEvent` carries `Email` (needed downstream for welcome mail); `PasswordChangedDomainEvent` carries only `UserId`+timestamp.

## Next Steps
Unblocks phase-03 (Application). Application commands consume these aggregates via repository interface defined in phase-03.
