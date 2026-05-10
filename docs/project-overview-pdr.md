# Project Overview (PDR)

> **Canonical source of truth:** `docs/Lexio_Complete_Documentation.docx` §1–§3.
> This file is a BE-focused excerpt. Refer to the docx for the full product spec.

## What is Lexio?

Lexio is a vocabulary learning platform using spaced repetition (SM-2 algorithm).
Users create flashcard decks, review cards on an AI-optimised schedule, and track progress
over time. The backend is a .NET 10 microservices system serving a Next.js 15 PWA.

## Backend Scope

| Service | Responsibility |
|---------|---------------|
| Identity | Auth (OpenIddict, JWT RS256), user registration/profile |
| Vocabulary | Deck/card CRUD, content storage (MongoDB) |
| Progress | SM-2 scheduling, review history, analytics |
| Notification | Email/push triggered by review schedule |
| Search | Full-text card search (Elasticsearch projection) |

## Key Decisions

- Architecture: see [`docs/architecture/`](architecture/README.md)
- Technology: .NET 10, EF Core 9, MassTransit 8, OpenTelemetry 1.11

## Phase Plan

Foundation bootstrap complete (phases 01–12). Next: Identity service implementation.
See [`docs/project-roadmap.md`](project-roadmap.md) for milestone timeline.
