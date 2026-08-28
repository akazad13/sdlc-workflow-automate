# Specification: Issue #102 — feat(storage): Add SQLite persistent storage

## 1. Problem Statement & Context
Feature Request:
1. Add TTL-based URL expiration with background cleanup worker.
2. Implement Token-Bucket Rate Limiting (10 req/min per IP) with 429 Retry-After headers.
3. Provide real-time analytics /api/v1/stats reporting active vs expired URLs and total visits.

## 2. Formalized Acceptance Criteria
- [x] **AC 1 (Functional Invariant)**: Ingested feature requirements for `feat(storage): Add SQLite persistent storage` parsed and scoped.
- [x] **AC 2 (Interface & Contracts)**: Domain interfaces and minimal API endpoints designed to satisfy specifications.
- [x] **AC 3 (Backward Compatibility)**: Zero breaking changes to existing endpoints or models.
- [x] **AC 4 (Automated Validation)**: 100% test coverage across functional unit tests and integration tests.

## 3. Non-Functional Requirements
- High-concurrency thread safety (ConcurrentDictionary / EF Core WAL).
- Sub-millisecond latency for URL redirection endpoints.
- Zero external infrastructure dependency for testing.