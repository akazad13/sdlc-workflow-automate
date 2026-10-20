# 📜 Engineering Constitution & Quality Principles

> **Version:** 1.1.0 | **Governance:** Strict Spec-Driven Development (SDD) Compliance

This document defines the non-negotiable architectural principles, engineering standards, and quality gates for all modules and services in the project.

---

## 1. Core Architectural Principles

### Principle I: Clean Architecture & Domain Isolation (NON-NEGOTIABLE)
- Every module or service must adhere to Clean Architecture (Hexagonal / Ports & Adapters) layer boundaries:
  - **Domain**: Pure business entities, value objects, domain events, and domain exceptions. Zero dependencies on external frameworks, ORMs, HTTP abstractions, or infrastructure libraries.
  - **Application**: Use cases, command/query handlers, business workflows, validation rules, and output ports/interfaces.
  - **Infrastructure**: Implementations of database persistence, message broker clients, external APIs, and caching.
  - **Api / Presentation**: Controllers or route handlers, middleware, request/response DTOs, and serialization.
- **Dependency Inversion**: Outer layers depend on inner layers; inner layers NEVER reference outer layers.

### Principle II: Test-First & Quality Gates (NON-NEGOTIABLE)
- Every feature or module specification must define verifiable Unit, Integration, and Automated E2E test criteria before implementation starts.
- Code without automated tests will be rejected by the Review Agent and CI pipeline.
- Quality standards:
  - Domain layer: minimum 85% branch coverage, measured by the coverage tool in CI (never estimated).
  - Endpoints: integration test verifying success (200/201), validation errors (400), unauthorized access (401/403), and missing resources (404).

### Principle III: Asynchronous Event-Driven Decoupling
- Services or modules must not perform synchronous cross-boundary writes during transactional flows.
- Inter-service state synchronization must occur asynchronously via domain events using the **Transactional Outbox Pattern** to prevent dual-write anomalies.

### Principle IV: Zero-Trust Security & Data Isolation
- All authenticated endpoints must validate cryptographic tokens (e.g., JWT Bearer) and enforce role/permission claims.
- Multi-tenant / customer data isolation must be enforced at the repository or query level. Users must never read or modify resources belonging to another tenant or owner.
- Secrets, credentials, or sensitive PII must never be committed to source code or logged in plain text.

### Principle V: Standardized Error Handling (RFC 7807)
- APIs must return standardized RFC 7807 Problem Details for all 4xx and 5xx errors.
- Internal exception stack traces and server internals must never be leaked to public API clients.

### Principle VI: Idempotency & Concurrency Protection
- State-altering operations (creation, checkout, payment processing, status updates) must support idempotency mechanisms (e.g. `Idempotency-Key` headers) to prevent duplicate execution.
- High-concurrency operations must enforce optimistic locking or atomic reservation patterns to eliminate race conditions.

### Principle VII: Traceability (NON-NEGOTIABLE)
- Every PRD requirement has a stable `REQ-NNN` ID and exactly one owner module.
- Every acceptance criterion (`<KEY>-AC-NN`) and E2E scenario (`<KEY>-E2E-NN`) traces to at least one requirement, is planned by at least one task, and is referenced by name or comment in at least one automated test.
- IDs are never renumbered or reused.

### Principle VIII: Conventions Before Invention
- In an existing codebase, new code follows the patterns recorded in `project-context.md`. Introducing a new pattern, framework, or third-party package requires a recorded reason in `plan.md` or a constitution amendment.
- Ambiguity is resolved by asking, not guessing: unresolved questions are written as `[NEEDS CLARIFICATION]` and block the spec gate.

### Principle IX: Small, Reviewable Changes
- A pull request stays within `max_pr_changed_lines` (see `.specify/config.json`). Larger modules ship as stacked PRs, each independently buildable and tested.
- Shared contracts and persisted schemas change in a backward-compatible way (expand, migrate, contract) unless a breaking change is approved.

### Principle X: Honest Verification
- Pass/fail claims come from tool output (`verification.md`, CI), never from an agent's own assessment.
- Tests are never deleted, skipped, focused, or weakened to make a suite pass. A necessary skip is justified in the review log.
- An agent that cannot reach approval within the iteration budget reports `BLOCKED`; it does not self-approve.

---

## 2. Review Process & CI/CD Gates

1. **Spec Gate**: `sdd_engine.py validate` reports 0 errors (no unresolved markers, full AC-to-task coverage, acyclic dependencies) and a human approves the spec.
2. **Independent Review**: The Review Agent runs in a separate context from the Generator, cites `file:line` evidence for every finding, and records an AC coverage matrix in `review-log.md`.
3. **Measured Verification**: `sdd_engine.py verify` passes on the exact code being shipped (fingerprint check). Lint and build pass with zero warnings.
4. **AI PR Review**: The AI reviewer check fails on Critical/Major findings or an incomplete review. Its findings are triaged, not blindly applied.
5. **Human Merge**: Merges to `main` need green required checks and at least one human approval. Agents never merge on their own.

---

## 3. Amendment Log

| Version | Date | Change |
| :--- | :--- | :--- |
| 1.0.0 | - | Initial principles I-VI |
| 1.1.0 | 2026-10-19 | Added principles VII-X (traceability, conventions, change size, honest verification) and human merge gate |
