# Specification: [MODULE TITLE] (`[module-slug]`)

**Module Key:** `[KEY]` | **Status:** Draft | **Depends on:** TODO(sdd): upstream module slugs or `None`

> ID rules: acceptance criteria use `[KEY]-AC-NN`, E2E scenarios use `[KEY]-E2E-NN`, PRD requirements use `REQ-NNN`.
> IDs are stable: never renumber an existing ID, only append. Every automated test must reference the ID(s) it verifies in its name or in a comment.
> Never guess: put a `[NEEDS CLARIFICATION: <question>]` marker in the body instead. `sdd_engine.py validate` fails while any such marker or TODO(sdd) placeholder remains outside these notes.

## 1. Executive Summary & Scope
- **Module Purpose**: TODO(sdd): 2-3 sentences on what this module does and the business value it delivers.
- **In Scope**: TODO(sdd): capabilities this module owns.
- **Out of Scope**: TODO(sdd): related capabilities explicitly owned elsewhere (name the owning module).
- **PRD Requirements Covered**: TODO(sdd): `REQ-001`, `REQ-002`

---

## 2. Personas & User Stories
- **US-01**: As a `[persona]`, I want to `[action]`, so that `[outcome]`. Traces: REQ-001

---

## 3. Acceptance Criteria (Given-When-Then)
- **[KEY]-AC-01 (Happy path)** Traces: REQ-001
  - **Given** TODO(sdd): valid authenticated state / preconditions
  - **When** TODO(sdd): client invokes endpoint/command with valid payload
  - **Then** TODO(sdd): observable result (status code, persisted state, emitted event)
- **[KEY]-AC-02 (Input validation failure)** Traces: REQ-001
  - **Given** missing, malformed, or out-of-range required fields
  - **When** the client submits the request
  - **Then** the system returns HTTP 400 with RFC 7807 Problem Details listing each invalid field
- **[KEY]-AC-03 (Unauthorized request)** Traces: REQ-001
  - **Given** missing or invalid credentials
  - **When** the client calls a protected resource
  - **Then** the system returns HTTP 401 and does not reveal whether the resource exists

---

## 4. Contracts
### 4.1 Contracts Provided (owned by this module)
- **Base Route**: `/api/v1/[module-slug]`
- **Security**: TODO(sdd): auth scheme and required roles/scopes
- **Headers**: `X-Correlation-ID` (propagated), `Idempotency-Key` (required for mutating operations)

| Method | Path | Request Body | Response Body | Status Codes | ACs |
| :--- | :--- | :--- | :--- | :--- | :--- |
| `POST` | `/api/v1/[module-slug]` | `Create[Entity]Request` | `[Entity]Response` | 201, 400, 401, 409 | [KEY]-AC-01, [KEY]-AC-02 |

- **Events Published**: TODO(sdd): topic, schema, delivery guarantee, or `None`

### 4.2 Contracts Consumed (owned by other modules)
| Provider Module | Contract (endpoint / event / interface) | Version | Failure Behaviour |
| :--- | :--- | :--- | :--- |
| TODO(sdd) | | | |

---

## 5. Data Models & State Machine
- **Primary Entity**: TODO(sdd): name, fields, types, constraints, indexes
- **State Transitions**: TODO(sdd): e.g. `Draft -> Active -> Completed | Cancelled`; list forbidden transitions
- **Migrations**: TODO(sdd): new tables/columns and whether they are backward compatible with the running version

---

## 6. Automated Test Criteria (MANDATORY GATE)

### 6.1 Unit Test Criteria
- [ ] Domain invariants for [KEY]-AC-01 (entity creation rejects invalid state).
- [ ] Validation rules for [KEY]-AC-02 (null, empty, boundary, and malformed values).

### 6.2 Integration Test Criteria
- [ ] Persistence round-trip for entities touched by [KEY]-AC-01.
- [ ] Auth middleware returns 401 for [KEY]-AC-03 and RFC 7807 bodies for [KEY]-AC-02.
- [ ] Migrations apply cleanly on an empty database and on a copy of the previous schema.

### 6.3 Automated End-to-End (E2E) Test Scenarios
- [ ] **[KEY]-E2E-01: Full lifecycle** (covers [KEY]-AC-01)
  1. Authenticate client and receive token.
  2. Create entity via `POST /api/v1/[module-slug]`, assert 201 and body.
  3. Read entity via `GET /api/v1/[module-slug]/{id}`, assert persisted state.
- [ ] **[KEY]-E2E-02: Fault injection & security** (covers [KEY]-AC-02, [KEY]-AC-03)
  1. POST invalid payload, assert 400 Problem Details.
  2. GET non-existent ID, assert 404.
  3. POST without token, assert 401.

---

## 7. Non-Functional & Security Requirements
- **Performance**: TODO(sdd): measurable thresholds (e.g. p95 < 200 ms at 50 rps) or `None`
- **Security**: TODO(sdd): OWASP risks relevant to this module and the mitigation for each
- **Observability**: logs, metrics, and traces this module must emit

---

## 8. Impact on Existing Code (brownfield)
- **Files / packages expected to change**: TODO(sdd): paths, or `None (greenfield)`
- **Shared contracts affected**: TODO(sdd): and the backward-compatibility plan
- **Risks**: TODO(sdd)

## 9. Open Questions
- None
