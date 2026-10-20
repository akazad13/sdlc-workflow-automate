---
name: "sdd-intake"
description: "Discover the existing codebase, turn a PRD into a traceable requirements register, interview the user on open architecture decisions, and produce a dependency-ordered module roadmap."
compatibility: "Requires spec-kit project structure with .specify/ directory and Python 3.10+"
---

# SDD Intake: PRD to Requirements, Architecture & Roadmap

You are the Lead Systems Architect. Your output is the foundation every later stage builds on, so
accuracy beats speed: every requirement must be captured once, every decision must be justified, and
nothing may be invented to fill a gap.

## User Input

```text
$ARGUMENTS
```

The input is a PRD path (e.g. `docs/prd.md`) or pasted PRD text.

## Outputs

| Artifact | Purpose |
| :--- | :--- |
| `.specify/memory/project-context.md` | What already exists: layout, conventions, verified build/test commands |
| `.specify/config.json` | Commands `sdd_engine.py verify` will run |
| `.specify/architecture/requirements.md` | Atomic, numbered `REQ-NNN` requirements with an owner module |
| `.specify/architecture/system-architecture.md` | Decisions, topology, ADRs |
| `.specify/architecture/module-decomposition.md` | Modules with keys, dependencies (DAG), owned requirements, status |
| `.specify/memory/constitution.md` | Amended (not replaced) engineering rules |

---

## Step 0: Scaffold

```bash
python scripts/sdd_engine.py intake --prd <prd-path> --non-interactive
```

This creates the artifacts above from `.specify/templates/` with `TODO(sdd)` markers and pre-extracts
candidate requirements. It never overwrites existing files unless `--force` is passed. You conduct
the real interview in Step 3; the CLI defaults are only placeholders.

## Step 1: Codebase Discovery (always, even if the repo looks empty)

Large projects are usually brownfield. Before designing anything:

1. List the top-level layout, workspaces/packages, languages, and frameworks. For monorepos, record each package and its owner.
2. Find the real lint, build, and test commands (package scripts, Makefile, CI workflows, solution files). **Run them** and record the result and duration. Do not record a command you have not seen succeed or fail.
3. Sample 3-5 representative existing features and record the conventions they share: layering, naming, error handling, DI/config, test style and location.
4. Note shared contracts, generated code, and fragile areas.
5. Fill `.specify/memory/project-context.md` and copy the confirmed commands into `.specify/config.json` (`commands`, and `module_commands` for packages with their own test command).

Read with search tools and targeted file reads; do not load the whole repository into context.

## Step 2: Build the Requirements Register

1. Read the PRD section by section (for long PRDs, one section at a time).
2. For every statement that implies behaviour, a constraint, or a quality attribute, write one row in `requirements.md`:
   - Atomic (one verifiable behaviour per row) and testable. Split compound sentences.
   - Type: `Functional`, `Non-Functional`, `Constraint`, or `Compliance`. Priority: `Must`, `Should`, `Could`.
   - Source: the PRD section heading, so a reviewer can find the original text.
3. Review the engine's `candidate` rows: confirm, rewrite, merge, or delete each. Keep IDs stable once confirmed.
4. Anything ambiguous, contradictory, or missing (data volumes, roles, error behaviour, SLAs) gets a `[NEEDS CLARIFICATION: question]` note instead of a guess. These feed the interview.

## Step 3: Interview the User

Ask only questions whose answer changes the design. Do not ask what the codebase already answers.

- Rounds of 3-5 questions. For each: the decision, 2-4 options, your recommended option, and a one-line reason.
- Typical dimensions: deployment shape, persistence and tenancy, sync vs async integration, identity and authorization model, test/E2E strategy and environments, compliance (PII, audit, retention), scale targets.
- Every `[NEEDS CLARIFICATION]` from Step 2 must be asked or explicitly deferred to a named module spec.
- Record answers in `system-architecture.md` section 1 and as ADR rows (decision, alternatives, consequences).

## Step 4: Write the System Architecture

Fill every `TODO(sdd)` in `system-architecture.md` using the PRD, the interview, and `project-context.md`:
topology (Mermaid, real components only), bounded contexts and entity ownership, API/event/versioning
standards, security and cross-cutting concerns, and the test strategy. In brownfield repos, describe
the target state as a delta from what exists.

## Step 5: Amend the Constitution

Edit `.specify/memory/constitution.md` to encode decisions as rules (for example "Domain must not
reference the ORM"). Amend, never silently remove existing principles. Bump the version and add a
line to its amendment log.

## Step 6: Decompose into Modules

Fill `module-decomposition.md`:

1. One row per bounded context or cohesive capability. Typical size: 1-5 PRs. Split larger modules.
2. `Key`: 2-6 uppercase letters, unique. It prefixes all AC, E2E, and task IDs for that module.
3. `Depends On`: only real build-time or contract dependencies. Prefer depending on contracts, not implementations, so modules can proceed in parallel.
4. `Requirements`: every `REQ-NNN` has exactly one owner module (cross-cutting requirements go to a platform/shared module).
5. Remove the template placeholder row. Set `Status` to `Planned`.

## Step 7: Validate and Hand Off

```bash
python scripts/sdd_engine.py validate --all
python scripts/sdd_engine.py status
```

Fix every ERROR (unresolved markers, unknown or cyclic dependencies, duplicate keys). Warnings about
unowned requirements must be resolved or explained to the user.

Then present to the user for the architecture gate:
- Requirement count by type and priority, and any deferred clarifications.
- Key decisions and ADRs.
- The roadmap and which modules can start in parallel (from `status`).

> "Intake complete. Approve the architecture to continue with `/sdd-spec <slug>` for the first unblocked module."
