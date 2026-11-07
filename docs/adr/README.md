# Architecture Decision Records (ADRs)

This directory contains Architecture Decision Records (ADRs) for the **Autonomous Spec-Driven SDLC Toolkit**.

We capture the *why* behind significant architectural choices, structural constraints, and technology selections. These records serve as an authoritative reference for both human engineers and autonomous AI agents (Antigravity, Claude Code, Cursor, GitHub Copilot).

---

## Decision Index

| Number | Title | Status | Date | Summary |
| :--- | :--- | :--- | :--- | :--- |
| [ADR-0001](0001-record-architecture-decisions.md) | Record Architecture Decisions | `Accepted` | 2026-10-20 | Adopt ADRs in `docs/adr/` to capture architectural decisions and prevent drift in multi-agent workflows. |
| [ADR-0002](0002-zero-dependency-stdlib-engine.md) | Zero-Dependency Python 3 Standard Library Engine | `Accepted` | 2026-10-20 | Restrict core CLI engine (`scripts/sdd_engine.py`) and review scripts to Python 3 stdlib without external packages. |
| [ADR-0003](0003-testing-paradigm-and-agentic-verification.md) | Testing Paradigm and Agentic Verification | `Accepted` | 2026-11-07 | Codify zero-dependency Python stdlib unittest testing paradigm, DAMP test design, Prove-It bug reproduction, and independent agentic verification. |

---

## ADR Lifecycle

```
PROPOSED ───► ACCEPTED ───┬───► SUPERSEDED (by ADR-XXXX)
                          └───► DEPRECATED
```

- **PROPOSED**: Decision under active review and discussion.
- **ACCEPTED**: Decision agreed upon and currently in effect.
- **SUPERSEDED**: Decision replaced by a subsequent ADR (which links back to the original).
- **DEPRECATED**: Decision retired without a direct replacement.

> [!NOTE]
> Historical ADRs are immutable. When an architectural pattern or requirement changes, author a new ADR that explicitly supersedes the earlier record rather than editing the historical decision.

---

## Authoring a New ADR

To propose a new decision:

1. Copy the structure below into `docs/adr/NNNN-<slug>.md` using the next sequential four-digit number.
2. Add an entry to the index table above.
3. Submit a pull request for review.

### Template

```markdown
# ADR-NNNN: [Short, Imperative Title]

## Status

Proposed | Accepted | Superseded by ADR-XXXX | Deprecated

## Date

YYYY-MM-DD

## Context

[What problem are we trying to solve? What are the requirements and constraints?]

## Decision

[What is the chosen approach? What change are we making?]

## Alternatives Considered

### [Alternative 1]
- **Pros**: [Advantages]
- **Cons**: [Disadvantages]
- **Rejected**: [Specific rationale for rejection]

### [Alternative 2]
- **Pros**: [Advantages]
- **Cons**: [Disadvantages]
- **Rejected**: [Specific rationale for rejection]

## Consequences

- **Positive**: [Benefits and improvements]
- **Trade-offs**: [Known drawbacks, compromises, or operational burdens]
- **Impact**: [Downstream requirements or conventions established]
```
