# 🚀 Autonomous Spec-Driven SDLC Toolkit

A production-tested, fully autonomous **Spec-Driven Development (SDD) AI Workflow** that transforms Product Requirements Documents (PRDs) into production code, comprehensive automated test suites (Unit, Integration, E2E), and verified GitHub Pull Requests with autonomous AI code reviews.

---

## 📦 What's in This Toolkit?

```text
sdlc-workflow-automate/
├── .agents/
│   └── skills/
│       ├── sdd-intake/SKILL.md         # Stage 1: PRD Ingestion & Architectural Interview
│       ├── sdd-spec/SKILL.md           # Stage 2: Spec generation with mandatory E2E criteria
│       ├── sdd-loop/SKILL.md           # Stage 3: Autonomous Generator <-> Reviewer refinement loop
│       └── sdd-pr/SKILL.md             # Stage 4: Branch, README sync, and GitHub PR creation
├── .github/
│   ├── scripts/
│   │   └── pr_review_agent.py          # Google AI Gemini PR Reviewer with line-by-line attribution
│   └── workflows/
│       ├── ci-template.yml             # Strict CI quality gate (lint, build --warnaserror, test)
│       └── pr-review-agent.yml         # GitHub Actions trigger for AI PR reviews
├── .specify/
│   ├── memory/
│   │   └── constitution.md             # Core engineering standards & layer boundaries
│   ├── templates/
│   │   ├── checklist-template.md       # Pre-flight quality checklist
│   │   ├── plan-template.md            # Technical architecture layout
│   │   ├── spec-template.md            # Given-When-Then + automated test criteria
│   │   └── tasks-template.md           # Dependency-ordered task list
│   └── workflows/
│       └── sdd-full/
│           └── workflow.yml            # Spec-Kit multi-agent pipeline definition
└── scripts/
    └── sdd_engine.py                   # Portable Python CLI orchestrator
```

---

## 🧰 Tools & Technologies Used

This toolkit leverages an integrated suite of AI engines, developer CLIs, CI/CD pipelines, and multi-language toolchains:

| Category | Tool / Technology | Role & Purpose in Workflow |
| :--- | :--- | :--- |
| **AI Orchestration** | **Antigravity AI Agent** | Executes autonomous agent skills (`.agents/skills/`), slash commands (`/sdd-intake`, `/sdd-spec`, `/sdd-loop`, `/sdd-pr`), and multi-agent coordination. |
| **AI PR Reviewer** | **[Google AI Gemini](https://aistudio.google.com/)** | Drives `.github/scripts/pr_review_agent.py` using models like `gemini-2.5-flash`, `gemini-2.0-flash`, and `gemini-1.5-pro` for automated, line-by-line PR reviews and merge gating. |
| **Spec Framework** | **Spec-Kit Standard** | Provides the `.specify/` specification structure, engineering constitution (`constitution.md`), quality checklists, and workflow pipelines (`workflow.yml`). |
| **CLI Orchestration** | **Python 3.11+ / `sdd_engine.py`** | Standalone, zero-dependency portable CLI orchestrator powering interactive PRD intake, spec generation, generator-auditor refinement, and verification. |
| **Terminal UI** | **[Rich](https://github.com/Textualize/rich)** | Formats CLI output with interactive tables, panels, and colored status indicators inside `sdd_engine.py` (with automatic fallback if not installed). |
| **Version Control & PRs** | **[GitHub CLI (`gh`)](https://cli.github.com/)** | Programmatically creates pull requests (`gh pr create`), reads review comments, and executes safe squash merges (`gh pr merge`). |
| **CI/CD Platform** | **[GitHub Actions](https://github.com/features/actions)** | Runs strict CI quality gates (`ci-template.yml`) and triggers the autonomous Gemini PR review workflow (`pr-review-agent.yml`). |
| **Source Control** | **[Git](https://git-scm.com/)** | Manages feature branches (`feature/<module-slug>`) and enforces conventional commit standards. |
| **Visual Architecture** | **[Mermaid.js](https://mermaid.js.org/)** | Renders sequence diagrams and architectural lifecycle flowcharts directly in markdown documentation. |
| **Standards & Contracts** | **[RFC 7807 (Problem Details)](https://datatracker.ietf.org/doc/html/rfc7807)** | Defines machine-readable API error payload contracts in module specifications and E2E test criteria. |

### Supported Language & Testing Toolchains
The toolkit's autonomous loop and CI template dynamically auto-detect and integrate with your ecosystem's native toolchain:
- **TypeScript / Node.js**: `npm`, `tsc --noEmit`, ESLint (`npm run lint`), Vitest / Jest (`npm test`).
- **Python**: `pip`, Ruff (`ruff check .`), Mypy (`mypy .`), Pytest (`pytest`).
- **Go**: `go build`, `golangci-lint`, `go test -v -race`.
- **.NET / C#**: `dotnet restore`, `dotnet build --warnaserror`, `dotnet test`.
- **Rust**: `cargo clippy -- -D warnings`, `cargo test`.

---

## ⚡ Quickstart Guide

### Prerequisites
Before running the workflow, ensure you have:
1. **Python 3.11+** installed (`python --version`).
2. **Git** configured (`git config user.name` & `git config user.email`).
3. **GitHub CLI (`gh`)** installed and authenticated (`gh auth login`).
4. *(Optional for CLI UI)*: `pip install rich` for colorized terminal tables.
5. *(For AI PR Review)*: A [Google AI Studio API Key](https://aistudio.google.com/) (`GEMINI_API_KEY`).

---

### Step 1: Add Toolkit to Your Target Project
You can either work directly inside this repository or copy the toolkit files into any existing project repository:

```bash
# In your target project repository:
cp -r /path_to_this_repo/.agents .
cp -r /path_to_this_repo/.github .
cp -r /path_to_this_repo/.specify .
cp -r /path_to_this_repo/scripts .
```

### Step 2: Configure GitHub Repository Secret
To enable the autonomous Google AI PR Reviewer:
1. Generate an API key from [Google AI Studio](https://aistudio.google.com/).
2. Navigate to your GitHub repository **Settings** ➔ **Secrets and variables** ➔ **Actions**.
3. Create a new repository secret named `GEMINI_API_KEY` and paste your key.

### Step 3: Run the Workflow!
You can run the SDLC workflow using either **Agent Slash Commands** or the **Standalone Python CLI**:

#### Option A: Inside Agent Chat / Antigravity
```text
1. /sdd-intake docs/prd.md       # Ingest PRD & complete interactive interview
2. /sdd-spec auth-service        # Generate module spec with automated E2E criteria
3. /sdd-loop auth-service        # Run Generator <-> Reviewer autonomous coding loop
4. /sdd-pr auth-service          # Run test suite, update README, and create GitHub PR
```

#### Option B: Standalone CLI
```bash
# Complete end-to-end pipeline in one command:
python scripts/sdd_engine.py run-all --prd docs/prd.md --module auth-service

# Or step-by-step:
python scripts/sdd_engine.py intake --prd docs/prd.md
python scripts/sdd_engine.py spec --module auth-service
python scripts/sdd_engine.py loop --module auth-service
python scripts/sdd_engine.py pr --module auth-service

# Handy flags:
#   --non-interactive : Apply recommended architectural defaults without prompt
#   --mock            : Run in simulation mode (useful for local smoke testing)
```

---

## 🔄 The 7-Stage SDLC Lifecycle

```mermaid
flowchart TD
    A[1. PRD Input] --> B[sdd-intake: Interactive Architectural Interview]
    B --> C[System Architecture & Module Roadmap]
    C --> D[sdd-spec: Formal Spec with Unit, Integration & E2E Criteria]
    D --> E[sdd-loop: Autonomous Multi-Agent Refinement Loop]
    E -->|Max 3 Cycles| F[Review Agent Audit & 100% Green Test Gate]
    F --> G[sdd-pr: README Sync, Git Branch & gh pr create]
    G --> H[GitHub Actions: Google AI PR Reviewer]
    H -->|CHANGES REQUESTED| I[Strict Merge Gate: Auto-Blocked]
    I -->|Remediate & Push| H
    H -->|APPROVED| J[Safe Squash Merge to Main]
```

### Stage 1: PRD Intake & Interactive Architectural Interview (`/sdd-intake`)
- Ingests user PRD (`docs/prd.md`).
- Conducts an interactive interview resolving architecture, database engine, messaging broker, authentication, and testing frameworks.
- Generates `.specify/architecture/system-architecture.md` and `.specify/architecture/module-decomposition.md`.

### Stage 2: Spec-Driven Feature Specification (`/sdd-spec`)
- Generates `.specify/specs/<module-slug>/spec.md`, `plan.md`, `tasks.md`, and `checklist.md`.
- **Mandatory E2E Testing Gate**: Section 6 explicitly defines Unit, Integration, and end-to-end user journeys (Happy Path + Fault Injection/RFC 7807 Problem Details).

### Stage 3: Generator ↔ Reviewer Iterative Refinement Loop (`/sdd-loop`)
- Orchestrates an autonomous loop between a **Code & Test Generator Agent** and an impartial **Review Agent (Auditor)**.
- Reviewer audits code across 5 pillars:
  1. *Spec Adherence*
  2. *Test Completeness*
  3. *Architecture & Standards*
  4. *Error & Edge Cases*
  5. *Security & Performance*
- Rejection feeds back specific diffs and remediation items (up to 3 iterations).
- Executes local test runner until 100% pass rate is achieved.
- Records full audit trail in `review-log.md`.

### Stage 4: Pre-Flight Verification & PR Creation (`/sdd-pr`)
- Verifies build and test pass with zero warnings.
- Synchronizes `README.md` (roadmap table, test counts, directory layout).
- Creates git branch `feature/<module-slug>`, commits with conventional commit, and raises GitHub PR via `gh`.

### Stage 5: In-Repo Google AI PR Reviewer (`pr-review-agent.yml`)
- Triggers on every PR open or update.
- Reads PR metadata, full unified diff, and `.specify/memory/constitution.md`.
- Posts line-by-line findings with concrete copy-pasteable patches.
- Strict verdict rubric:
  - `❌ CHANGES REQUESTED`: Constitution violation, security vulnerability, test gap, or build warning. Exits with code 1, **blocking merge**.
  - `⚠️ APPROVED WITH SUGGESTIONS`: Non-blocking ergonomic or styling suggestions.
  - `✅ APPROVED`: Zero defects, 100% test coverage.

### Stage 6: Peer Feedback & Remediation Loop
- AI agent ingests review comments: `gh pr view <pr-number> --comments`.
- Triages each finding against the constitution, applies code patch, runs test suite, commits, pushes, and re-triggers AI review until approved.

### Stage 7: Strict Merge Gate & Stacked PR Strategy
- Squash merges to `main` only when all CI checks are green and verdict is approved.
- Allows stacked branches (`feature/module-2` branched from `feature/module-1`) so teams and agents are never blocked on pending reviews.

---

## 🗺️ Module Roadmap & Implementation Status

As modules progress through `/sdd-spec`, `/sdd-loop`, and `/sdd-pr`, their status, branches, PR links, and test metrics are synchronized here:

| Module Slug | Scope / Title | Feature Branch | GitHub PR | Review Gate | Test Coverage |
| :--- | :--- | :--- | :--- | :--- | :--- |
| *`auth-service`* *(example)* | *JWT Authentication & Session Management* | `feature/auth-service` | Pending PR | `✅ APPROVED` | 100% Passed (Unit, E2E) |

### 📚 Specifications & Governance Documents
- **Engineering Constitution**: [`.specify/memory/constitution.md`](.specify/memory/constitution.md) — Layer boundaries, clean architecture standards, and non-negotiable quality principles.
- **Pipeline Definition**: [`.specify/workflows/sdd-full/workflow.yml`](.specify/workflows/sdd-full/workflow.yml) — Spec-Kit pipeline stages, inputs, and approval gates.
- **Spec & Task Templates**: [`.specify/templates/`](.specify/templates/) — Pre-flight checklists, technical plans, Given-When-Then specs, and task lists.

---

## 🛠️ Multi-Language Stack Customization

The toolkit automatically detects your runtime in `scripts/sdd_engine.py` and supports:

| Ecosystem | Linter / Build Command | Test Runner |
| :--- | :--- | :--- |
| **Node.js / TypeScript** | `npm run lint` / `tsc --noEmit` | `npm test` (Vitest / Jest) |
| **Python** | `ruff check .` / `mypy .` | `pytest` |
| **Go** | `golangci-lint run` | `go test ./...` |
| **.NET / C#** | `dotnet build --warnaserror` | `dotnet test` |
| **Rust** | `cargo clippy -- -D warnings` | `cargo test` |

Edit `.specify/memory/constitution.md` to encode your language-specific rules and layer boundaries.

