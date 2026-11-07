# AGENTS.md — AI Agent Guidance & Repository Rules

This document provides mandatory guidance for autonomous AI coding agents (Antigravity, Claude Code, GitHub Copilot, Cursor, Windsurf, Devin) operating in `akazad13/sdlc-automate-workflow`.

---

## 1. System Identity & Architecture

**Product**: `Autonomous Spec-Driven SDLC Toolkit` — Reusable Spec-Driven Development (SDD) AI workflow turning PRDs into traceable specs, verified code, and reviewable pull requests.
- **Engine**: Zero-dependency Python 3 CLI engine (`scripts/sdd_engine.py`).
- **Skills**: `.agents/skills/` (`sdd-intake`, `sdd-spec`, `sdd-loop`, `sdd-pr`).
- **GitHub Automation**: `.github/workflows/` (CI merge gate, Gemini AI PR review agent).
- **Templates & Constitution**: `.specify/` templates and `.specify/memory/constitution.md`.
- **Architecture Decision Records (ADRs)**: [`docs/adr/`](docs/adr/README.md) ([ADR-0001](docs/adr/0001-record-architecture-decisions.md), [ADR-0002](docs/adr/0002-zero-dependency-stdlib-engine.md)).

---

## 2. Inviolable Architectural Invariants

1. **Zero External Dependencies in Core Engine ([ADR-0002](docs/adr/0002-zero-dependency-stdlib-engine.md))**:
   - `scripts/sdd_engine.py` and auxiliary tooling must remain strictly Python standard library (no `pip install` required). It must execute cleanly on any fresh machine or runner with standard Python 3.10+.
2. **Deterministic Traceability**:
   - Traceability must be strictly bidirectional: `REQ-NNN` (Requirement) -> `AC-NNN` (Acceptance Criterion) -> `E2E-NNN` (Test) -> `TASK-NNN` (Implementation Task).
3. **Independent Verification**:
   - The engine must measure verification independently; it never relies on self-reported agent assertions.
4. **Git Branch & Gate Integrity**:
   - Reusable workflows and templates must pass `scripts/sdd_engine.py validate --all` before release.
5. **Architecture Decisions Recorded as ADRs ([ADR-0001](docs/adr/0001-record-architecture-decisions.md))**:
   - Any architectural change, framework choice, or structural invariant modification must be accompanied by an ADR in `docs/adr/` and indexed in `docs/adr/README.md`. Decisions are immutable; changes require superseding ADRs.

---

## 3. Commands Cheat Sheet

```bash
# Run stdlib engine self-tests
python -m unittest discover -s tests/toolkit -p "test_*.py"

# Validate SDD configuration and templates
python scripts/sdd_engine.py validate --all

# Run intake workflow
python scripts/sdd_engine.py intake --prd docs/PRD.md
```

---

## 4. Verification Gates for AI Agents

Before submitting code changes or finishing a turn:
1. Run `python -m unittest discover -s tests/toolkit -p "test_*.py"` — all regression tests must pass.
2. Confirm that `scripts/sdd_engine.py` retains 100% standard library compliance without adding third-party imports ([ADR-0002](docs/adr/0002-zero-dependency-stdlib-engine.md)).
3. Validate that any architectural or invariant alterations are documented in [`docs/adr/`](docs/adr/README.md) per [ADR-0001](docs/adr/0001-record-architecture-decisions.md).
