# AGENTS.md — AI Agent Guidance & Repository Rules

This document provides mandatory guidance for autonomous AI coding agents (Antigravity, Claude Code, GitHub Copilot, Cursor, Windsurf, Devin) operating in `akazad13/sdlc-automate-workflow`.

---

## 1. System Identity & Architecture

**Product**: `Autonomous Spec-Driven SDLC Toolkit` — Reusable Spec-Driven Development (SDD) AI workflow turning PRDs into traceable specs, verified code, and reviewable pull requests.
- **Engine**: Zero-dependency Python 3 CLI engine (`scripts/sdd_engine.py`).
- **Skills**: `.agents/skills/` (`sdd-intake`, `sdd-spec`, `sdd-loop`, `sdd-pr`).
- **GitHub Automation**: `.github/workflows/` (CI merge gate, Gemini AI PR review agent).
- **Templates & Constitution**: `.specify/` templates and `.specify/memory/constitution.md`.
- **Architecture Decision Records (ADRs)**: [`docs/adr/`](docs/adr/README.md) ([ADR-0001](docs/adr/0001-record-architecture-decisions.md), [ADR-0002](docs/adr/0002-zero-dependency-stdlib-engine.md), [ADR-0003](docs/adr/0003-testing-paradigm-and-agentic-verification.md), [ADR-0004](docs/adr/0004-cross-agent-plugin-architecture.md)).

---

## 2. Inviolable Architectural Invariants

1. **Zero External Dependencies in Core Engine ([ADR-0002](docs/adr/0002-zero-dependency-stdlib-engine.md))**:
   - `scripts/sdd_engine.py` and auxiliary tooling must remain strictly Python standard library (no `pip install` required). It must execute cleanly on any fresh machine or runner with standard Python 3.10+.
2. **Deterministic Traceability**:
   - Traceability must be strictly bidirectional: `REQ-NNN` (Requirement) -> `AC-NNN` (Acceptance Criterion) -> `E2E-NNN` (Test) -> `TASK-NNN` (Implementation Task).
3. **Independent Verification & Pure Stdlib Testing ([ADR-0003](docs/adr/0003-testing-paradigm-and-agentic-verification.md))**:
   - The engine measures verification independently from actual process exit codes and test runner outputs; it never relies on self-reported agent assertions.
   - Toolkit self-tests must strictly use Python standard library `unittest` (zero `pip install`).
4. **Git Branch & Gate Integrity**:
   - Reusable workflows and templates must pass `scripts/sdd_engine.py validate --all` before release.
5. **Architecture Decisions Recorded as ADRs ([ADR-0001](docs/adr/0001-record-architecture-decisions.md))**:
   - Any architectural change, framework choice, or structural invariant modification must be accompanied by an ADR in `docs/adr/` and indexed in `docs/adr/README.md`. Decisions are immutable; changes require superseding ADRs.
6. **Cross-Agent Plugin Standard Integrity ([ADR-0004](docs/adr/0004-cross-agent-plugin-architecture.md))**:
   - The plugin manifests (`plugin.json`, `.claude-plugin/plugin.json`, `.agents/plugins/marketplace.json`, `.gemini/commands/`) must strictly match the active skills in `.agents/skills/` and adhere to the Agent Plugin schema. Customizations must install and execute cleanly in Antigravity, Claude Code, and open skills harnesses.

---

## 3. Testing Paradigm & Agentic Verification ([ADR-0003](docs/adr/0003-testing-paradigm-and-agentic-verification.md))

Autonomous AI agents must adhere to the following testing standards:
- **Pure Standard Library Runner**: All tests in `tests/toolkit/` must execute via Python's built-in `unittest` runner with zero third-party dependencies (`pytest`, `mock`, etc.).
- **DAMP over DRY**: Prioritize Descriptive And Meaningful Phrases over DRY abstractions. Tests must be self-contained and immediately readable upon failure.
- **The Prove-It Pattern**: For every bug fix or behavior change, first write a failing unit test reproducing the issue (RED) before implementing the fix (GREEN).
- **Objective Measurement**: Never accept self-attested agent claims ("all tests pass"). Verification is only valid when measured by `scripts/sdd_engine.py verify` with explicit process exit codes, parsed test counts, and code fingerprints.
- **Bidirectional Traceability**: Every requirement and acceptance criterion must map directly to an automated test case.

---

## 4. Commands Cheat Sheet

```bash
# Run stdlib engine self-tests (95+ unit tests, zero dependencies)
python -m unittest discover -s tests/toolkit -p "test_*.py"

# Validate SDD configuration and templates
python scripts/sdd_engine.py validate --all

# Run intake workflow
python scripts/sdd_engine.py intake --prd docs/PRD.md

# Agent slash commands (when installed as a plugin)
/sdd-intake   # PRD intake, requirements register, interview, module DAG
/sdd-spec     # Traceable module spec, plan with delivery slices, tasks
/sdd-loop     # TDD execution loop, multi-axis review, measured verify
/sdd-pr       # Verification gates, roadmap sync, right-sized/stacked PR
```

---

## 5. Verification Gates for AI Agents

Before submitting code changes or finishing a turn:
1. Run `python -m unittest discover -s tests/toolkit -p "test_*.py"` — all regression tests must pass.
2. Confirm that `scripts/sdd_engine.py` and tests retain 100% standard library compliance without adding third-party imports ([ADR-0002](docs/adr/0002-zero-dependency-stdlib-engine.md), [ADR-0003](docs/adr/0003-testing-paradigm-and-agentic-verification.md)).
3. Confirm bug fixes include a Prove-It reproduction test per [ADR-0003](docs/adr/0003-testing-paradigm-and-agentic-verification.md).
4. Validate that any architectural or invariant alterations are documented in [`docs/adr/`](docs/adr/README.md) per [ADR-0001](docs/adr/0001-record-architecture-decisions.md).
5. Ensure plugin manifests (`plugin.json`, `.claude-plugin/plugin.json`, `.agents/plugins/marketplace.json`, `.gemini/commands/`) remain valid and in sync ([ADR-0004](docs/adr/0004-cross-agent-plugin-architecture.md)).
