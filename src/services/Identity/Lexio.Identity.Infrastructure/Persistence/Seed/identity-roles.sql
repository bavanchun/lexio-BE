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
