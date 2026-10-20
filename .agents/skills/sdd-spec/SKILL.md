---
name: "sdd-spec"
description: "Generate a traceable module specification (REQ -> AC -> E2E -> task IDs), a convention-aware technical plan with delivery slices, and a dependency-ordered task list, then validate it."
compatibility: "Requires spec-kit project structure with .specify/ directory and Python 3.10+"
---

# SDD Spec: Traceable Module Specification

You are the Lead Specification Engineer. A spec is accurate when every owned requirement becomes at
least one observable acceptance criterion, every criterion has a planned task and a planned test,
and nothing in it is guessed.

## User Input

```text
$ARGUMENTS
```

The module slug (e.g. `auth-service`). If omitted, run `python scripts/sdd_engine.py status` and
pick the first module whose dependencies are `Done` and whose status is `Planned`.

---

## Step 1: Load Context

1. `.specify/memory/constitution.md` and `.specify/memory/project-context.md`.
2. `.specify/architecture/system-architecture.md`, the module's row in `module-decomposition.md`, and
   the rows of `requirements.md` it owns.
3. For each module in `Depends On`: its `spec.md` section 4.1 (Contracts Provided). Consume those
   contracts exactly; do not redefine them.
4. Brownfield: search the codebase for code this module will touch or reuse (entities, routes,
   tables, shared services). Read those files, not the whole repo.

## Step 2: Scaffold

```bash
python scripts/sdd_engine.py spec --module <slug>
```

Creates `spec.md`, `plan.md`, `tasks.md`, `checklist.md` under `.specify/specs/<slug>/` with the
module key from the roadmap. Existing files are kept unless `--force` is passed.

## Step 3: Write `spec.md`

Replace every `TODO(sdd)`. Rules:

- **IDs**: `<KEY>-AC-NN` for acceptance criteria, `<KEY>-E2E-NN` for E2E scenarios. Never renumber;
  only append. Each AC line ends with `Traces: REQ-NNN[, ...]`.
- **Coverage**: every owned `REQ-NNN` appears in at least one AC. Cover the happy path, validation
  failures, authorization failures, not-found, conflicts/concurrency, and idempotent retries where
  the requirement implies them.
- **Observable outcomes**: "Then" states a status code, response field, persisted state, or emitted
  event. Reject vague outcomes such as "works correctly" or "handles errors".
- **Contracts**: section 4.1 lists everything this module provides (with the ACs each endpoint
  satisfies); 4.2 lists consumed contracts with provider module and failure behaviour.
- **Section 6** maps tests to criteria: unit, integration, and E2E items name the AC/E2E IDs they verify.
- **Section 8 (brownfield impact)**: files expected to change, shared contracts affected, and the
  backward-compatibility plan.
- **Unknowns**: write `[NEEDS CLARIFICATION: question]`. Do not resolve it by guessing; ask the user.

## Step 4: Write `plan.md`

- Follow the patterns in `project-context.md`. A new pattern or package requires a constitution
  amendment or a recorded reason.
- List reused components and files to modify with paths.
- **Delivery slices**: split the module into PR-sized slices under `max_pr_changed_lines` from
  `.specify/config.json` (default 600). Each slice is independently buildable and tested.
- Migration, feature-flag, and rollback strategy for anything touching persisted data or public contracts.
- The targeted test command for this module; add it to `module_commands` in `.specify/config.json`.

## Step 5: Write `tasks.md`

Format: `- [ ] <KEY>-TNNN [P] Description (AC: ids) (Files: paths) (Depends: task ids | none)`

- Every AC and E2E ID is referenced by at least one task.
- Tasks are small (roughly one file group and its tests, under ~150 changed lines).
- `[P]` only when the task touches files no concurrent task touches.
- Order by dependency; tests are part of the task that implements the behaviour, not a separate late phase.

## Step 6: Validate (mandatory)

```bash
python scripts/sdd_engine.py validate --module <slug>
```

Iterate until it reports 0 errors. Then walk `checklist.md` and tick only items you can justify.
If any `[NEEDS CLARIFICATION]` remains, stop and ask the user; validation will keep failing until it
is answered.

## Step 7: Gate

Set the module's roadmap status to `Specified` and present:
- Requirements covered, AC and E2E counts, and the slice plan.
- Contracts provided and consumed, and any breaking-change risk.
- Open questions answered during this step.

> "Specification for `<slug>` is ready in `.specify/specs/<slug>/`. Approve to start `/sdd-loop <slug>`."
