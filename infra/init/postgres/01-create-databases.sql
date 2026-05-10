-- Creates per-service databases for local development.
-- This script runs only on first volume initialization (Postgres docker-entrypoint-initdb.d).
CREATE DATABASE identity_db;
CREATE DATABASE vocabulary_db;
CREATE DATABASE learning_db;
CREATE DATABASE statistics_db;
CREATE DATABASE notifications_db;
CREATE DATABASE social_db;
