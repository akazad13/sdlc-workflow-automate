# Technical Implementation Plan: [MODULE TITLE] (`[module-slug]`)

> **Module Key:** `[KEY]` | **Spec:** `.specify/specs/[module-slug]/spec.md`

## 1. Architectural Alignment
- **Pattern**: TODO(sdd): pattern from `system-architecture.md` (do not introduce a new one without a constitution amendment)
- **Layer Mapping**:
  - `Domain`: entities, value objects, domain events, domain exceptions. No framework dependencies.
  - `Application`: command/query handlers, use cases, validators, ports.
  - `Infrastructure`: persistence, external clients, message publishers.
  - `Api / Presentation`: routes, auth filters, middleware, DTOs.

---

## 2. Existing Code Analysis (required for brownfield repos)
- **Conventions to follow**: TODO(sdd): naming, folder layout, error handling, DI registration, test style observed in `project-context.md`
- **Reused components**: TODO(sdd): existing services, base classes, helpers (with paths)
- **Files to modify**: TODO(sdd): paths and why

---

## 3. Directory Layout & Proposed Code Tree
```text
TODO(sdd): match the repo's existing layout. Example:
src/[module-slug]/
  Domain/
  Application/
  Infrastructure/
  Api/
tests/[module-slug]/
  Unit/
  Integration/
  E2E/
```

---

## 4. Technology & Dependencies
- **New third-party packages**: TODO(sdd): name, version, licence, reason, or `None`
- **Runtime / ORM / Validation / Test runners**: as defined in `system-architecture.md`

---

## 5. Delivery Slices
Large modules ship as a stack of small PRs (target: under `max_pr_changed_lines` from `.specify/config.json`).

| Slice | Scope | Tasks | Est. changed lines |
| :--- | :--- | :--- | :--- |
| 1 | Contracts, domain, unit tests | [KEY]-T001..[KEY]-T00N | TODO(sdd) |

---

## 6. Data Migration & Rollout
- **Migration strategy**: TODO(sdd): expand/contract, backfill, or `None`
- **Feature flag**: TODO(sdd): flag name, or `None`
- **Rollback**: TODO(sdd)

---

## 7. Verification Commands
- **Targeted tests for this module**: TODO(sdd): e.g. `pytest tests/[module-slug]` (also set `module_commands` in `.specify/config.json`)
- **Full gate**: `python scripts/sdd_engine.py verify --module [module-slug]`
