CREATE TABLE IF NOT EXISTS public.__ef_migrations_history (
    migration_id character varying(150) NOT NULL,
    product_version character varying(32) NOT NULL,
    CONSTRAINT pk___ef_migrations_history PRIMARY KEY (migration_id)
);

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "migration_id" = '20260514165416_InitialIdentitySchema') THEN
    CREATE TABLE outbox_messages (
        id uuid NOT NULL,
        type character varying(500) NOT NULL,
        payload text NOT NULL,
        occurred_at timestamp with time zone NOT NULL,
        processed_at timestamp with time zone,
        CONSTRAINT pk_outbox_messages PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "migration_id" = '20260514165416_InitialIdentitySchema') THEN
    CREATE TABLE roles (
        id uuid NOT NULL DEFAULT (uuidv7()),
        name character varying(64) NOT NULL,
        description character varying(500) NOT NULL,
        permissions jsonb NOT NULL,
        CONSTRAINT pk_roles PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "migration_id" = '20260514165416_InitialIdentitySchema') THEN
    CREATE TABLE users (
        id uuid NOT NULL DEFAULT (uuidv7()),
        email character varying(255) NOT NULL,
        password_hash text,
        display_name character varying(100) NOT NULL,
        role_id uuid NOT NULL,
        status character varying(32) NOT NULL,
        is_verified boolean NOT NULL,
        email_verified_at timestamp with time zone,
        last_login_at timestamp with time zone,
        banned_reason character varying(500),
        banned_at timestamp with time zone,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        created_by character varying(64),
        is_deleted boolean NOT NULL,
        deleted_at timestamp with time zone,
        CONSTRAINT pk_users PRIMARY KEY (id),
        CONSTRAINT ck_users_email_lowercase CHECK (email = lower(email)),
        CONSTRAINT fk_users_roles_role_id FOREIGN KEY (role_id) REFERENCES roles (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "migration_id" = '20260514165416_InitialIdentitySchema') THEN
    CREATE TABLE oauth_connections (
        id uuid NOT NULL DEFAULT (uuidv7()),
        user_id uuid NOT NULL,
        provider character varying(32) NOT NULL,
        provider_user_id character varying(128) NOT NULL,
        connected_at timestamp with time zone NOT NULL,
        last_used_at timestamp with time zone,
        CONSTRAINT pk_oauth_connections PRIMARY KEY (id),
        CONSTRAINT fk_oauth_connections_users_user_id FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "migration_id" = '20260514165416_InitialIdentitySchema') THEN
    CREATE TABLE refresh_tokens (
        id uuid NOT NULL DEFAULT (uuidv7()),
        user_id uuid NOT NULL,
        token_hash character varying(128) NOT NULL,
        expires_at timestamp with time zone NOT NULL,
        issued_at timestamp with time zone NOT NULL,
        revoked_at timestamp with time zone,
        ip_address character varying(64),
        CONSTRAINT pk_refresh_tokens PRIMARY KEY (id),
        CONSTRAINT fk_refresh_tokens_users_user_id FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "migration_id" = '20260514165416_InitialIdentitySchema') THEN
    CREATE UNIQUE INDEX ix_oauth_connections_provider_provider_user_id ON oauth_connections (provider, provider_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "migration_id" = '20260514165416_InitialIdentitySchema') THEN
    CREATE INDEX ix_oauth_connections_user_id ON oauth_connections (user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "migration_id" = '20260514165416_InitialIdentitySchema') THEN
    CREATE INDEX ix_outbox_messages_processed_at ON outbox_messages (processed_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "migration_id" = '20260514165416_InitialIdentitySchema') THEN
    CREATE UNIQUE INDEX ix_refresh_tokens_token_hash ON refresh_tokens (token_hash);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "migration_id" = '20260514165416_InitialIdentitySchema') THEN
    CREATE INDEX ix_refresh_tokens_user_id_revoked_at ON refresh_tokens (user_id, revoked_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "migration_id" = '20260514165416_InitialIdentitySchema') THEN
    CREATE UNIQUE INDEX ix_roles_name ON roles (name);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "migration_id" = '20260514165416_InitialIdentitySchema') THEN
    CREATE UNIQUE INDEX ix_users_email_unique ON users (email);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "migration_id" = '20260514165416_InitialIdentitySchema') THEN
    CREATE INDEX ix_users_role_id ON users (role_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "migration_id" = '20260514165416_InitialIdentitySchema') THEN
    -- Idempotent role seed for Lexio Identity.
    -- The 5 UUIDs below are STABLE identifiers — never rotate them. They are
    -- foreign-keyed by users.role_id and referenced as Role.SeedIds constants
    -- in the Domain layer.
    INSERT INTO roles (id, name, description, permissions) VALUES
      ('a1d4f0b0-0001-7000-8000-000000000001', 'Guest',           'Anonymous access; can only read public content',          '["public:read"]'),
      ('a1d4f0b0-0001-7000-8000-000000000002', 'Learner',         'Default authenticated user; can study and track progress', '["vocab:read","study:write","profile:write"]'),
      ('a1d4f0b0-0001-7000-8000-000000000003', 'VerifiedCreator', 'Email-verified creator; can publish decks',                '["vocab:read","study:write","profile:write","deck:write"]'),
      ('a1d4f0b0-0001-7000-8000-000000000004', 'Moderator',       'Community moderation: flag and unflag content',            '["vocab:read","study:write","profile:write","deck:write","moderation:write"]'),
      ('a1d4f0b0-0001-7000-8000-000000000005', 'Admin',           'Full system administration',                               '["*"]')
    ON CONFLICT (id) DO UPDATE
      SET name        = EXCLUDED.name,
          description = EXCLUDED.description,
          permissions = EXCLUDED.permissions;

    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "migration_id" = '20260514165416_InitialIdentitySchema') THEN
    INSERT INTO public.__ef_migrations_history (migration_id, product_version)
    VALUES ('20260514165416_InitialIdentitySchema', '9.0.10');
    END IF;
END $EF$;
COMMIT;

