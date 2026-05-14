# Phase 03 — Application layer (Mediator, validators, RBAC)

## Context Links
- researcher-04 §1 (endpoints/DTOs), §3 (validation), §7 (JWT claims)
- BuildingBlocks: `Lexio.BuildingBlocks.Abstractions` (`IUnitOfWork`, `Result<T>`, `IClock`)
- Mediator (martinothamar) source-generator pinned in foundation phase-02

## Overview
- Priority: P1
- Status: pending
- Effort: 4h
- Branch: `feat/be-identity-application` (off phase-02)
- PR: stacked PR #15

Use-case layer. Commands, queries, validators, DTOs, repository interfaces, RBAC authorization policies. Pure C# — no AspNetCore, no EF, no MassTransit.

## Key Insights
- Commands return `Result<T>`; never throw for business failures (auth-failed, conflict).
- Validators run via Mediator pipeline behaviour `ValidationBehavior<TReq, TRes>` (shared infra in BuildingBlocks).
- `IUserRepository` defined here; implemented in Infrastructure (phase-04).
- `IPasswordHasher` abstraction here; bcrypt impl in Infrastructure (phase-05).
- `ITokenIssuer` abstraction here; OpenIddict impl in Infrastructure (phase-05).
- Claims: standard `sub`, `email`, `name`, `role`, plus custom `permissions` array — assembled in `ITokenIssuer.Issue(user, role)`.

## Requirements
**Functional commands/queries**
1. `RegisterUserCommand(email, password, displayName)` → `Result<AuthResponseDto>`
2. `LoginCommand(email, password, ipAddress)` → `Result<AuthResponseDto>`
3. `RefreshTokenCommand(refreshToken)` → `Result<RefreshResponseDto>`
4. `LogoutCommand(userId)` → `Result` (revokes all active refresh tokens)
5. `GetMeQuery(userId)` → `Result<UserDto>`
6. `UpdateProfileCommand(userId, displayName?, currentPassword?, newPassword?)` → `Result<UserDto>`
7. `GetRolesQuery()` → `Result<IReadOnlyList<RoleDto>>`
8. `ChangeUserRoleCommand(targetUserId, newRoleId, adminUserId)` → `Result` (Admin-only, future use; included now for completeness)

**RBAC policies (registered in Application DI for reuse in Api)**
- `RequireLearner`, `RequireVerifiedCreator`, `RequireModerator`, `RequireAdmin` — claims-based; each policy checks `role` claim against role hierarchy.
- `RequirePermission(string permissionName)` — generic policy reading `permissions` claim.

**Validators (FluentValidation)**
- `RegisterUserCommandValidator`: email regex, password rules (≥8, ≥1 digit, ≥1 special), displayName 1–100.
- `LoginCommandValidator`: email format, non-empty password.
- `UpdateProfileCommandValidator`: at least one field, password rules if `newPassword` set, requires `currentPassword` when `newPassword` set.
- `RefreshTokenCommandValidator`: non-empty.

**Non-functional**
- No project reference to `Microsoft.AspNetCore.*` or `Microsoft.EntityFrameworkCore.*`.
- Mapster profile maps `User → UserDto`, `Role → RoleDto`.

## Architecture
```
Lexio.Identity.Application/
├── Features/
│   ├── Auth/
│   │   ├── Register/ (Command, Handler, Validator, Dto)
│   │   ├── Login/
│   │   ├── Refresh/
│   │   ├── Logout/
│   │   └── Me/
│   ├── Users/
│   │   ├── UpdateProfile/
│   │   └── ChangeRole/
│   └── Roles/
│       └── List/
├── Contracts/
│   ├── Persistence/
│   │   ├── IUserRepository.cs
│   │   ├── IRoleRepository.cs
│   │   └── IRefreshTokenRepository.cs
│   ├── Security/
│   │   ├── IPasswordHasher.cs        (Hash, Verify)
│   │   └── ITokenIssuer.cs           (IssueAccessToken, IssueRefreshToken)
│   └── Auditing/
│       └── IAuditLogger.cs           (LogAsync(eventType, payload))
├── Common/
│   ├── Behaviors/
│   │   ├── ValidationBehavior.cs     (Mediator pipeline)
│   │   └── LoggingBehavior.cs
│   ├── Mappings/MapsterConfig.cs
│   ├── Errors/IdentityErrors.cs      (static Result error factories)
│   └── Authorization/Policies.cs
└── DependencyInjection.cs
```

## Related Code Files
**Create:** ~32 files in Application project.
**Modify:** `Lexio.Identity.Api/Program.cs` — call `AddIdentityApplication()` (added in phase-06 wiring).
**Delete:** none.

## Implementation Steps
1. Define repository interfaces returning domain types (`User`, `Role`, `RefreshToken`).
2. Define `IPasswordHasher`, `ITokenIssuer`, `IAuditLogger`.
3. Implement 8 commands/queries with Mediator `IRequestHandler<TReq, TRes>`; each handler does: validate → load aggregate → mutate → `unitOfWork.SaveChangesAsync` → return `Result`.
4. `LoginCommandHandler` records login (`user.RecordLogin(ip, clock)`) inside the same transaction as token issuance; emits `UserLoggedIn` audit event via `IAuditLogger`.
5. `RegisterUserCommandHandler` checks email uniqueness via `IUserRepository.EmailExistsAsync`; returns `IdentityErrors.EmailAlreadyExists` (409).
6. `RefreshTokenCommandHandler`: hashes incoming token, looks up active record, validates not revoked + not expired, rotates (revoke old, issue new) — returns 401 on any mismatch using generic error (no enumeration).
7. `UpdateProfileCommandHandler`: verifies current password via `IPasswordHasher.Verify`; if new password supplied, hashes + calls `user.ChangePassword(newHash, clock)`.
8. Write FluentValidation validators per requirements.
9. Configure Mapster profile.
10. Register all via `AddIdentityApplication` extension.
11. Application.Tests: handler unit tests with mocked repos + hashers (Moq); ≥80% coverage.
12. PR #15 stacked on phase-02 branch.

## Todo List
- [ ] 8 commands/queries with handlers
- [ ] 4 validators
- [ ] 3 repository interfaces
- [ ] `IPasswordHasher`, `ITokenIssuer`, `IAuditLogger`
- [ ] Mapster profile
- [ ] RBAC policy definitions
- [ ] Mediator pipeline behaviours
- [ ] Application.Tests ≥80% coverage
- [ ] PR #15 opened

## Success Criteria
- `dotnet test tests/Lexio.Identity.Application.Tests` green.
- NetArchTest: Application has no reference to AspNetCore or EF (phase-08 enforces).
- All 8 handlers covered by at least: 1 happy-path + 1 validation-failure + 1 domain-failure test.

## Risk Assessment
| Risk | L | I | Mitigation |
|---|---|---|---|
| Validator/Handler duplication of rules | M | L | Validators do shape/format only; handlers do business invariants. Document in CLAUDE.md addendum. |
| Refresh-token rotation race (concurrent refresh from two tabs) | M | H | Pessimistic lock via `SELECT ... FOR UPDATE` in repository (phase-04). Plan annotates. |
| Generic 401 leaks timing info | L | M | Hash a dummy password in `Verify` when user not found (constant-time path). |

## Security Considerations
- Never include `PasswordHash` in any DTO.
- `ITokenIssuer.IssueRefreshToken` returns raw token to caller exactly once; persists bcrypt hash only.
- `LoginCommandHandler` always invokes `IPasswordHasher.Verify` (even on missing user) to prevent user-enumeration via timing.
- Audit event `LoginFailed` emitted with `email` + `ipAddress`, never with attempted password.

## Next Steps
Unblocks phase-04 (Infrastructure implements `IUserRepository` against EF) and phase-05 (Infrastructure implements `IPasswordHasher` + `ITokenIssuer`).
