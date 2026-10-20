# Implementation Tasks: [MODULE TITLE] (`[module-slug]`)

> Format: `- [ ] [KEY]-TNNN [P?] Description (AC: ids) (Files: paths) (Depends: task ids | none)`
> - `[P]` marks tasks that touch disjoint files and can run in parallel.
> - Every acceptance criterion and E2E scenario in `spec.md` must be referenced by at least one task.
> - Keep each task small enough to implement and test in one sitting (roughly under 150 changed lines).
> - Tick a task only after its tests pass locally. Never tick tasks in bulk.

## Phase 1: Contracts & Domain
- [ ] [KEY]-T001 Create domain entities and invariants (AC: [KEY]-AC-01) (Files: TODO(sdd)) (Depends: none)
- [ ] [KEY]-T002 [P] Define request/response DTOs and error mappings (AC: [KEY]-AC-02) (Files: TODO(sdd)) (Depends: none)

## Phase 2: Core Logic & Unit Tests
- [ ] [KEY]-T003 Implement validation and use case handlers with unit tests (AC: [KEY]-AC-01, [KEY]-AC-02) (Files: TODO(sdd)) (Depends: [KEY]-T001, [KEY]-T002)

## Phase 3: Infrastructure, API & Integration Tests
- [ ] [KEY]-T004 Implement persistence, migrations, and repository with integration tests (AC: [KEY]-AC-01) (Files: TODO(sdd)) (Depends: [KEY]-T003)
- [ ] [KEY]-T005 Implement API routes and auth policies with integration tests (AC: [KEY]-AC-02, [KEY]-AC-03) (Files: TODO(sdd)) (Depends: [KEY]-T003)

## Phase 4: Automated E2E Tests
- [ ] [KEY]-T006 Implement [KEY]-E2E-01 and [KEY]-E2E-02 (AC: [KEY]-E2E-01, [KEY]-E2E-02) (Files: TODO(sdd)) (Depends: [KEY]-T004, [KEY]-T005)

## Phase 5: Verification & Documentation
- [ ] [KEY]-T007 Run `sdd_engine.py verify --module [module-slug]` and resolve every failure (Depends: [KEY]-T006)
- [ ] [KEY]-T008 Update README roadmap row and docs links (Depends: [KEY]-T007)
