# Quality Checklist: [MODULE TITLE] (`[module-slug]`)

> Tick an item only with evidence. Items marked (auto) are enforced by `python scripts/sdd_engine.py validate` / `verify`.

## 1. Specification Readiness
- [ ] (auto) No `[NEEDS CLARIFICATION]` or `TODO(sdd)` markers remain in spec, plan, or tasks.
- [ ] Every user story and acceptance criterion traces to at least one `REQ-NNN` in `requirements.md`.
- [ ] Acceptance criteria are observable (status codes, persisted state, events), not vague ("works correctly").
- [ ] In-scope and out-of-scope boundaries are explicit and agree with `module-decomposition.md`.
- [ ] Consumed contracts list the provider module and its failure behaviour.

## 2. Task Plan
- [ ] (auto) Every `[KEY]-AC-NN` and `[KEY]-E2E-NN` is referenced by at least one task.
- [ ] (auto) Task dependencies reference existing tasks and contain no cycles.
- [ ] Each task names the files it touches and is small enough for one review sitting.

## 3. Architectural & Security Compliance
- [ ] Plan follows existing repo conventions recorded in `project-context.md`.
- [ ] Layer boundaries respected (Domain has no framework dependencies).
- [ ] No secrets, credentials, or unmasked PII in code, config, or logs.
- [ ] Authorization enforced on every mutating route; idempotency on state-changing operations.

## 4. Verification & Delivery
- [ ] (auto) Every AC / E2E ID is referenced by at least one automated test.
- [ ] (auto) Lint, build, and full test suite pass (`verification.md` shows PASS).
- [ ] (auto) No focused (`.only`) tests; any newly skipped test is justified in `review-log.md`.
- [ ] No tests deleted or assertions weakened to make the suite pass.
- [ ] Review Agent has recorded `STATUS: APPROVED` with evidence in `review-log.md`.
- [ ] PR diff is within `max_pr_changed_lines`, or the module is split into stacked PRs.
