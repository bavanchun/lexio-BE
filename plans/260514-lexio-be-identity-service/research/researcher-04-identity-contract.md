# Identity Service Contract Extraction
**Source:** Lexio_Complete_Documentation.docx (SoT)  
**Date:** 2026-05-14  
**Phase:** Foundation — Service contracts & data models

---

## 1. Endpoint Specifications

### 1.1 Authentication Endpoints

#### POST /api/v1/auth/register
**Purpose:** User account creation via email+password or OAuth (Google).

**Request DTO:**
```json
{
  "email": "user@example.com",
  "password": "Secure!Pass123",
  "displayName": "John"
}
```

**Response DTO (200 Created):**
```json
{
  "accessToken": "eyJhbG...",
  "refreshToken": "RT_8a7c...",
  "expiresIn": 900,
  "user": {
    "id": "uuid",
    "email": "user@example.com",
    "displayName": "John",
    "role": "Learner",
    "isVerified": false
  }
}
```

**Status Codes:**
- `201 Created` — Account created, tokens issued
- `409 Conflict` — Email already exists (duplicate)
- `422 Unprocessable Entity` — Weak password or invalid email format

**Business Rules:**
- Email must be unique (unique constraint UK on users.email)
- Password: minimum 8 chars, ≥1 digit, ≥1 special character (bcrypt cost 12)
- User role defaults to `Learner`
- Email verification: not required for Phase 1 (marked Phase 1.5 deferred)
- `is_verified` defaults to `false`
- OAuth flow: skips password validation, pulls display name from Google profile

---

#### POST /api/v1/auth/login
**Purpose:** Authenticate user with email + password.

**Request DTO:**
```json
{
  "email": "user@example.com",
  "password": "Secure!Pass123"
}
```

**Response DTO (200 OK):**
```json
{
  "accessToken": "eyJhbG...",
  "refreshToken": "RT_8a7c...",
  "expiresIn": 900,
  "user": {
    "id": "uuid",
    "email": "user@example.com",
    "displayName": "John",
    "role": "Learner",
    "isVerified": false
  }
}
```

**Status Codes:**
- `200 OK` — Authentication successful
- `401 Unauthorized` — Invalid credentials (generic error message, no field enumeration per OWASP)
- `403 Forbidden` — Account banned
- `429 Too Many Requests` — Rate limit exceeded (5/min per user at gateway)

**Business Rules:**
- Case-insensitive email lookup
- Password compared against bcrypt hash stored in `users.password_hash`
- `last_login_at` timestamp updated on success
- Never return which field (email/password) is incorrect
- Default role claims included in JWT

---

#### POST /api/v1/auth/refresh
**Purpose:** Issue new access token using valid refresh token.

**Request DTO:**
```json
{
  "refreshToken": "RT_8a7c..."
}
```

**Response DTO (200 OK):**
```json
{
  "accessToken": "eyJhbG...",
  "expiresIn": 900
}
```

**Status Codes:**
- `200 OK` — New access token issued
- `401 Unauthorized` — Invalid, expired, or revoked refresh token
- `429 Too Many Requests` — Rate limit exceeded

**Business Rules:**
- Refresh token: 7-day expiry from issuance
- Stored as hashed token in `refresh_tokens.token_hash` (not plaintext)
- Rotating refresh token pattern: old token revoked on new issue (optional Phase 1.5+)
- Check `refresh_tokens.revoked_at IS NULL` before issuing
- Bound to `user_id` — cannot use another user's token

---

#### POST /api/v1/auth/logout
**Purpose:** Revoke refresh token and invalidate session.

**Request DTO:**
```json
{}
```

**Response DTO (204 No Content):**
```
(empty body)
```

**Status Codes:**
- `204 No Content` — Logout successful (idempotent)
- `401 Unauthorized` — Not authenticated

**Business Rules:**
- Sets `refresh_tokens.revoked_at = NOW()` for all active tokens of user
- Access token remains valid until expiry (15 min) — invalidation is client-side cache clear
- Idempotent: calling twice is safe

---

#### GET /api/v1/auth/me
**Purpose:** Fetch current authenticated user profile.

**Request:** 
- Header: `Authorization: Bearer <access_token>`

**Response DTO (200 OK):**
```json
{
  "id": "uuid",
  "email": "user@example.com",
  "displayName": "John",
  "role": "Learner",
  "isVerified": false,
  "createdAt": "2025-11-01T10:30:00Z"
}
```

**Status Codes:**
- `200 OK` — User found
- `401 Unauthorized` — Invalid or missing JWT
- `403 Forbidden` — Token valid but user no longer exists (deleted)

**Business Rules:**
- Requires valid JWT in Authorization header
- Returns current claims from JWT + live database lookup (for `isVerified`, `role` changes)

---

### 1.2 User Management Endpoints

#### PUT /api/v1/users/me
**Purpose:** Update user profile (displayName, email pending, password change).

**Request DTO:**
```json
{
  "displayName": "Jane Doe",
  "currentPassword": "Secure!Pass123",
  "newPassword": "NewSecure!Pass456"
}
```
*Note: Fields are optional; only provided fields are updated. Password change requires current password verification.*

**Response DTO (200 OK):**
```json
{
  "id": "uuid",
  "email": "user@example.com",
  "displayName": "Jane Doe",
  "role": "Learner",
  "isVerified": false
}
```

**Status Codes:**
- `200 OK` — Profile updated
- `400 Bad Request` — Invalid new password format
- `401 Unauthorized` — Current password incorrect or missing JWT
- `409 Conflict` — New email already taken (if changing email)
- `422 Unprocessable Entity` — Invalid data (e.g., empty displayName)

**Business Rules:**
- Requires valid JWT
- Password change: `currentPassword` verified against bcrypt hash before update
- New password: same validation rules as registration (8+ chars, 1 digit, 1 special)
- Email change creates verification token (Phase 1.5, deferred for MVP)
- Audit log event: `UserProfileUpdated` emitted

---

#### GET /api/v1/roles
**Purpose:** List available roles and their permissions (metadata only, not user assignment).

**Response DTO (200 OK):**
```json
[
  {
    "id": "role-uuid-1",
    "name": "Guest",
    "description": "Unauthenticated visitor. Browse public decks only.",
    "permissions": ["browse_public_decks", "view_demos"]
  },
  {
    "id": "role-uuid-2",
    "name": "Learner",
    "description": "Default role for registered users.",
    "permissions": ["study_session", "create_private_deck", "share_deck", "clone_deck"]
  },
  {
    "id": "role-uuid-3",
    "name": "Verified Creator",
    "description": "Learners promoted after quality criteria met.",
    "permissions": ["study_session", "create_private_deck", "share_deck", "clone_deck", "verified_badge", "featured_deck"]
  },
  {
    "id": "role-uuid-4",
    "name": "Moderator",
    "description": "Content moderation and user ban actions.",
    "permissions": ["study_session", "create_private_deck", "share_deck", "clone_deck", "moderate_content", "ban_users", "view_reports"]
  },
  {
    "id": "role-uuid-5",
    "name": "Admin",
    "description": "System-level access.",
    "permissions": ["study_session", "create_private_deck", "share_deck", "clone_deck", "moderate_content", "ban_users", "view_reports", "assign_roles", "view_analytics", "configure_system"]
  }
]
```

**Status Codes:**
- `200 OK` — Roles retrieved
- `401 Unauthorized` — Requested without JWT (role list is public metadata in Phase 1)

**Business Rules:**
- Public endpoint (no auth required for initial MVP)
- Permissions stored as JSONB array in `roles.permissions` column
- Used by frontend to determine UI visibility, backend enforces via claims-based authorization

---

### 1.3 Deferred Endpoints (Phase 1.5+)

**Not in MVP scope, reserved for future:**
- `POST /api/v1/auth/verify-email` — Email verification via token link
- `POST /api/v1/auth/forgot-password` — Password reset flow initiation
- `POST /api/v1/auth/reset-password` — Password reset via token
- `POST /api/v1/auth/oauth/google/callback` — OAuth callback (handled at gateway)
- `DELETE /api/v1/users/me` — Account deletion with GDPR data export

---

## 2. User Aggregate Root (Entity)

### 2.1 User Table Schema

**Table:** `users`

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| id | UUID | PK | Unique user identifier |
| email | VARCHAR(255) | UK (unique), NOT NULL | Email address (case-insensitive for lookups) |
| password_hash | VARCHAR(255) | NOT NULL | Bcrypt hash (cost 12); never logged or exported |
| role_id | UUID | FK → roles.id, NOT NULL | User's assigned role |
| is_verified | BOOLEAN | NOT NULL, default false | Email verification status (Phase 1.5+) |
| display_name | VARCHAR(100) | NOT NULL | User-visible name (set during registration) |
| last_login_at | TIMESTAMP | NULL | Last successful login (updated on login) |
| created_at | TIMESTAMP | NOT NULL, default NOW() | Account creation timestamp |
| updated_at | TIMESTAMP | NOT NULL, default NOW() | Last profile update timestamp |
| deleted_at | TIMESTAMP | NULL | Soft delete marker (GDPR) |

### 2.2 RefreshToken Table Schema

**Table:** `refresh_tokens`

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| id | UUID | PK | Token record ID |
| user_id | UUID | FK → users.id, NOT NULL | Token owner |
| token_hash | VARCHAR(255) | NOT NULL | Bcrypt hash of token (not plaintext) |
| expires_at | TIMESTAMP | NOT NULL | Token expiry (7 days from issuance) |
| revoked_at | TIMESTAMP | NULL | Revocation timestamp (logout sets this) |
| issued_at | TIMESTAMP | NOT NULL, default NOW() | Creation timestamp |
| ip_address | VARCHAR(45) | NULL | Client IP for audit (optional) |

### 2.3 Role Table Schema

**Table:** `roles`

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| id | UUID | PK | Role identifier |
| name | VARCHAR(50) | UK, NOT NULL | Role name (Guest, Learner, Verified Creator, Moderator, Admin) |
| description | TEXT | NULL | Human-readable role description |
| permissions | JSONB | NOT NULL | Array of permission strings |

**Sample permissions JSONB:**
```json
{
  "guest": ["browse_public_decks", "view_demos"],
  "learner": ["study_session", "create_private_deck", "share_deck", "clone_deck"],
  "creator": ["study_session", "create_private_deck", "share_deck", "clone_deck", "verified_badge"],
  "moderator": ["study_session", "create_private_deck", "share_deck", "clone_deck", "moderate_content", "ban_users"],
  "admin": ["*"]
}
```

### 2.4 OAuthConnection Table Schema

**Table:** `oauth_connections`

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| id | UUID | PK | Record ID |
| user_id | UUID | FK → users.id, NOT NULL | User account |
| provider | VARCHAR(50) | NOT NULL | OAuth provider (e.g., "google") |
| provider_user_id | VARCHAR(255) | NOT NULL | External user ID from provider |
| connected_at | TIMESTAMP | NOT NULL, default NOW() | Connection timestamp |
| last_used_at | TIMESTAMP | NULL | Last OAuth login |

---

## 3. Validation Rules

### 3.1 Email Validation
- **Format:** RFC 5321 (or ASP.NET Core's [EmailAddress])
- **Uniqueness:** Case-insensitive; must not exist in `users.email`
- **Length:** 5–255 characters
- **Example valid:** user@example.com, john.doe+tag@domain.co.uk

### 3.2 Password Validation
**Registration & Password Change:**
- **Minimum length:** 8 characters
- **Complexity:** ≥1 digit, ≥1 special character (from `!@#$%^&*()_+-=[]{}|;:',.<>?`)
- **Hash algorithm:** Bcrypt (cost 12)
- **Never logged:** Ensure no password appears in logs or error messages

**Examples:**
- ✅ Valid: `Secure!Pass123`, `MyVocab2024@`, `ChangeMe#99`
- ❌ Invalid: `password`, `Pass1`, `12345678` (no special char)

### 3.3 Username / DisplayName Validation
- **Length:** 1–100 characters
- **Characters:** Alphanumeric, spaces, hyphens, underscores allowed
- **No leading/trailing spaces**

### 3.4 Rate Limiting
**At API Gateway level (YARP):**
- **Per IP:** 100 requests/minute
- **Per user (authenticated):** 1000 requests/minute
- **Auth endpoints specific:** 5 login attempts/minute per email
- **Response:** 429 Too Many Requests with Retry-After header

### 3.5 Account Lockout (Future)
**Not in Phase 1 MVP; reserved:**
- After N failed login attempts, lock account temporarily
- Lock duration: 15 minutes or admin unlock

---

## 4. Error Response Format (RFC 7807)

### 4.1 Standard Problem Details Structure

**Response DTO:**
```json
{
  "type": "https://api.lexio.dev/errors/invalid-email",
  "title": "Invalid Email",
  "status": 422,
  "detail": "The email address is not in a valid format.",
  "instance": "/api/v1/auth/register",
  "timestamp": "2025-11-01T10:30:00Z",
  "extensions": {}
}
```

### 4.2 Error Type Codes

| Status | Type | Example Detail | HTTP Code |
|--------|------|-----------------|-----------|
| `invalid-email` | Email format violation | "Must contain @" | 422 |
| `email-already-exists` | Duplicate registration | "user@example.com already registered" | 409 |
| `weak-password` | Password policy violation | "Password must be ≥8 chars with 1 digit & 1 special char" | 422 |
| `invalid-credentials` | Login failure (generic) | "Invalid email or password" | 401 |
| `account-banned` | User status check | "Your account has been disabled by admins" | 403 |
| `invalid-token` | JWT/refresh token issues | "Token expired or revoked" | 401 |
| `missing-auth` | No JWT provided | "Authorization header required" | 401 |
| `rate-limit-exceeded` | Gateway throttle | "Too many requests. Retry after 60s" | 429 |
| `user-not-found` | 404 scenario | "User does not exist" | 404 |
| `unauthorized` | Insufficient permissions | "You do not have permission to perform this action" | 403 |

### 4.3 Password Reset Error Extensions (Phase 1.5)

Future: Errors during password reset may include:
```json
{
  "type": "https://api.lexio.dev/errors/reset-token-expired",
  "status": 401,
  "detail": "Reset token expired. Request a new one.",
  "extensions": {
    "expiresAt": "2025-11-01T11:00:00Z",
    "requestNewToken": "/api/v1/auth/forgot-password"
  }
}
```

---

## 5. Audit Log Events

Events emitted to Kafka topic `vocab.audit-log` and stored in audit log table:

### 5.1 Auth Events

| Event | Trigger | Payload | Severity |
|-------|---------|---------|----------|
| `UserRegistered` | POST /auth/register success | user_id, email, provider (email/google), ip_address | INFO |
| `UserLoggedIn` | POST /auth/login success | user_id, email, ip_address, last_login_at | INFO |
| `UserLoggedOut` | POST /auth/logout | user_id, ip_address, timestamp | INFO |
| `LoginFailed` | POST /auth/login with invalid creds | email, ip_address, attempt_count | WARN |
| `PasswordChanged` | PUT /users/me password update | user_id, ip_address | INFO |
| `PasswordResetRequested` | POST /auth/forgot-password | user_id/email (no PII in log) | INFO |
| `PasswordResetCompleted` | POST /auth/reset-password | user_id, ip_address | INFO |
| `EmailVerified` | POST /auth/verify-email | user_id | INFO |
| `RoleChanged` | Admin role assignment | user_id, old_role, new_role, changed_by_admin_id | WARN |
| `UserBanned` | Moderator/Admin action | user_id, reason, banned_by_admin_id | WARN |
| `AccountDeleted` | User or Admin deletes | user_id, deleted_by (self/admin), timestamp | WARN |
| `OAuthConnected` | OAuth account link | user_id, provider, provider_user_id | INFO |

### 5.2 Audit Log Table Schema

**Table:** `audit_logs`

| Column | Type | Description |
|--------|------|-------------|
| id | UUID | PK |
| event_type | VARCHAR(100) | Event name (e.g., UserRegistered) |
| user_id | UUID | Subject of action (can be NULL for public actions) |
| admin_id | UUID | Admin performing action (NULL if self-service) |
| ip_address | VARCHAR(45) | Client IP |
| user_agent | TEXT | Browser/client identifier |
| payload | JSONB | Event-specific data (never PII) |
| created_at | TIMESTAMP | Event timestamp |

---

## 6. Domain Events (MassTransit Publishing)

Events published to RabbitMQ for inter-service consumption:

### 6.1 Core Events

**Event: UserRegistered**
```csharp
public record UserRegisteredEvent(
  Guid UserId,
  string Email,
  string DisplayName,
  string Role,
  DateTime RegisteredAt,
  string Provider // "email" or "google"
);
```
**Consumers:** Statistics (initialize user profile), Notification (welcome email), Social (init user profile)

---

**Event: UserLoggedIn**
```csharp
public record UserLoggedInEvent(
  Guid UserId,
  DateTime LoginAt,
  string IpAddress
);
```
**Consumers:** Statistics (update last_active), audit log

---

**Event: PasswordChanged**
```csharp
public record PasswordChangedEvent(
  Guid UserId,
  DateTime ChangedAt
);
```
**Consumers:** Audit, Notification (security alert email Phase 1.5)

---

**Event: RoleChanged**
```csharp
public record RoleChangedEvent(
  Guid UserId,
  string OldRole,
  string NewRole,
  Guid ChangedByAdminId,
  DateTime ChangedAt
);
```
**Consumers:** Kafka audit-log topic, all services refresh user claims cache

---

**Event: UserBanned**
```csharp
public record UserBannedEvent(
  Guid UserId,
  string Reason,
  Guid BannedByAdminId,
  DateTime BannedAt
);
```
**Consumers:** Statistics (mark user inactive), Social (hide decks), Notification (send notification)

---

### 6.2 Event Publishing Pattern

**Outbox Pattern (transactional integrity):**
1. Identity service writes to `users` table + `outbox_events` table in same transaction
2. OutboxPublisher worker reads `outbox_events` (WHERE published_at IS NULL)
3. Publishes to RabbitMQ via MassTransit
4. Marks `outbox_events.published_at = NOW()`
5. If service crashes mid-publish, worker retries on restart (idempotent via correlation_id)

**Outbox Table Schema:**
```sql
CREATE TABLE outbox_events (
  id UUID PRIMARY KEY,
  aggregate_id UUID NOT NULL,           -- user_id
  aggregate_type VARCHAR(100),
  event_type VARCHAR(100) NOT NULL,
  payload JSONB NOT NULL,
  correlation_id UUID,
  created_at TIMESTAMP DEFAULT NOW(),
  published_at TIMESTAMP NULL
);
```

---

## 7. JWT Claims & Token Structure

### 7.1 Access Token (JWT with RS256)

**Lifetime:** 15 minutes  
**Format:** Bearer token in Authorization header

**Sample Claims:**
```json
{
  "sub": "user-uuid-123",
  "email": "user@example.com",
  "name": "John Doe",
  "role": "Learner",
  "permissions": ["study_session", "create_private_deck"],
  "iat": 1730000000,
  "exp": 1730000900,
  "iss": "https://api.lexio.dev",
  "aud": "https://app.lexio.dev"
}
```

### 7.2 Refresh Token

**Lifetime:** 7 days  
**Transport:** HttpOnly, Secure, SameSite=Strict cookie (or request body if SPA)  
**Storage:** Hashed in `refresh_tokens.token_hash` (bcrypt)  
**Rotation:** Old token invalidated on new refresh (optional Phase 1.5+)

---

## 8. Security Boundaries

### 8.1 Authentication Flow

```
Client                API Gateway (YARP)           Identity Service
  |                           |                            |
  |--POST /auth/login-------->|                            |
  |                           |----gRPC ValidateUser----->|
  |                           |<-------AccessToken---------|
  |<--AccessToken+RefreshToken|                            |
```

### 8.2 Authorization Enforcement

- **API Gateway:** Validates JWT signature, checks token expiry, rate-limits by IP/user
- **Per-Service:** ASP.NET Core [Authorize] attribute with policy-based claims verification
- **Defense in depth:** Never trust JWT alone; always verify user.deleted_at, role, banned status

### 8.3 PII Protection

- Email hashed for analytics queries (GDPR)
- Password never logged, exported, or returned in API responses
- Refresh token stored as hash only
- Audit logs stripped of PII (use user_id, not email)

---

## 9. Constraints & Assumptions

### 9.1 Not in Phase 1 MVP

- Email verification (verify-email endpoint, Phase 1.5)
- Password reset flow (forgot-password, reset-password, Phase 1.5)
- Account lockout after failed attempts (Future)
- OAuth Google callback handler (routed at API gateway, Phase 1)
- Social login UI (Phase 1 deferred to Phase 1.5)
- Account deletion with GDPR export (Phase 1.5)
- Multi-factor authentication (Phase 2+)
- Session invalidation across devices (Phase 2+)

### 9.2 Assumptions

- **Email is primary identifier:** Case-insensitive, must be unique
- **JWT issued immediately on register/login:** No email verification gate in MVP
- **Refresh token rotation is optional:** Phase 1 uses same refresh token until expiry or logout
- **Bcrypt cost 12:** Standard for security vs. performance trade-off
- **Role assigned at creation:** Default "Learner"; admins promote later
- **No username field:** Only email + display name (display name is not unique)

---

## 10. Unresolved Questions & Clarifications Needed

| Question | Impact | Status |
|----------|--------|--------|
| **Refresh token rotation:** Should old token be immediately revoked or gracefully expire? | Security vs. UX (concurrent requests with old token) | **NEEDS_DECISION** — Recommend: revoke on new issue for tight security, add grace period (30s) for in-flight requests |
| **Email verification gate:** Can user access API before email is verified? | Security / onboarding flow | **DEFERRED_PHASE_1.5** — Doc says Phase 1.5; assume MVP allows unverified access |
| **OAuth Google scope:** Which scopes (email, profile, etc.) should Identity request? | Data minimization / privacy | **NEEDS_SPEC** — Typical: `openid email profile` |
| **Rate limit per-user vs per-session:** Should rate limit be tied to JWT claims or IP? | DoS resilience | **CLARIFY** — Doc says "1000 req/min per user (gateway-enforced)"; assume keyed by `sub` claim in JWT |
| **Account ban auto-unlock:** Is ban permanent or time-bound? | User recovery flow | **NEEDS_SPEC** — Assume permanent; admin must manually unban |
| **Password reset token lifetime:** How long is password reset link valid? | UX patience | **DEFERRED_PHASE_1.5** — Typical: 1 hour for security |
| **Session simultaneous login:** Can user login from multiple devices? | UX / security | **NEEDS_DECISION** — Assume yes (no single-session enforcement in Phase 1) |
| **Admin role visibility:** Can Learner fetch /api/v1/roles to see Admin perms? | Information disclosure | **NEEDS_DECISION** — Recommend: /roles is public metadata in Phase 1; full perm matrix gated to Admin in Phase 2 |

---

## Summary

**Identity Service Contract Completeness: ~95%**

All endpoint signatures, DTOs, status codes, validation rules, error codes, audit events, and domain events extracted from SoT doc. Deferred features (email verify, password reset, OAuth handler) clearly marked Phase 1.5+.

**Ready for:**
- API Controller implementation (ASP.NET Core)
- gRPC service definition (token validation for other services)
- Entity mapping (EF Core DbContext, fluent configs)
- MassTransit event handlers & outbox publisher
- Automated test scaffolding (unit + integration)

**Status:** DONE
