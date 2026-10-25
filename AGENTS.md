# AGENTS.md — AI Agent Guidance & Repository Rules

This document provides mandatory guidance for autonomous AI coding agents (Antigravity, Claude Code, GitHub Copilot, Cursor, Windsurf, Devin) operating in `akazad13/sdlc-automate-workflow`.

---

## 1. System Identity & Architecture

**Product**: `Autonomous Spec-Driven SDLC Toolkit` — Reusable Spec-Driven Development (SDD) AI workflow turning PRDs into traceable specs, verified code, and reviewable pull requests.
- **Engine**: Zero-dependency Python 3 CLI engine (`scripts/sdd_engine.py`).
- **Skills**: `.agents/skills/` (`sdd-intake`, `sdd-spec`, `sdd-loop`, `sdd-pr`).
- **GitHub Automation**: `.github/workflows/` (CI merge gate, Gemini AI PR review agent).
- **Templates & Constitution**: `.specify/` templates and `.specify/memory/constitution.md`.

---

## 2. Inviolable Architectural Invariants

1. **Zero External Dependencies in Core Engine**:
   - `scripts/sdd_engine.py` must remain stdlib-only Python (no `pip install` required). It must execute on any fresh machine or runner with standard Python 3.8+.
2. **Deterministic Traceability**:
   - Traceability must be strictly bidirectional: `REQ-NNN` (Requirement) -> `AC-NNN` (Acceptance Criterion) -> `E2E-NNN` (Test) -> `TASK-NNN` (Implementation Task).
3. **Independent Verification**:
   - The engine must measure verification independently; it never relies on self-reported agent assertions.
4. **Git Branch & Gate Integrity**:
   - Reusable workflows and templates must pass `scripts/sdd_engine.py validate` before release.

---

## 3. Commands Cheat Sheet

```bash
# Run stdlib engine self-tests
python -m unittest discover -s tests -p "*_test.py"

# Validate SDD configuration and templates
python scripts/sdd_engine.py validate

# Run intake workflow
python scripts/sdd_engine.py intake --prd docs/PRD.md
```

---

## 4. Verification Gates for AI Agents

Before submitting code changes or finishing a turn:
1. Run `python -m unittest discover -s tests -p "*_test.py"` — all regression tests must pass.
2. Confirm that `scripts/sdd_engine.py` retains 100% standard library compliance without adding third-party imports.
