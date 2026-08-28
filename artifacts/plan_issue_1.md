# Implementation Plan: Issue #1 — feat(storage): Implement SQLite / EF Core 10 persistent repository with configurable storage provider

## 1. Component Architecture & Target Scopes
- **Scope & Focus**: `feat(storage): Implement SQLite / EF Core 10 persistent repository with configurable storage provider`
- **Target Projects**:
  - `src/NanoLink.Api`: ASP.NET Core Minimal APIs, Domain Models, Service Contracts, and Storage.
  - `src/NanoLink.Orchestrator`: CLI automation runner and quality gate evaluators.
  - `tests/NanoLink.Tests`: Comprehensive xUnit unit & integration testing matrices.

## 2. Test Verification Matrix
| Test Suite | Scope | Target Invariants |
| :--- | :--- | :--- |
| `UrlShortenerTests` | Unit | Base62 generation, custom alias validation, collision retry, TTL calculation |
| `RateLimiterTests` | Unit | Token acquisition, exhaustion, per-IP isolation, bucket reset |
| `ExpirationTests` | Unit | Expired URL blocking, background cleanup eviction verification |
| `IntegrationTests` | Integration | End-to-end WebApplicationFactory tests covering all HTTP status codes |

## 3. Git Branching Strategy
- Target Branch: `main`
- Working Branch: `feat/issue-1-featstorage-implement-sqlite-e`