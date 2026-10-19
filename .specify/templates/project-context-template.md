# Project Context (Codebase Discovery)

> Written by `/sdd-intake` before any design work, and refreshed when the repo structure changes.
> Agents read this before planning so generated code matches what already exists instead of inventing new patterns.

## 1. Repository Shape
- **Type**: TODO(sdd): greenfield | single service | monorepo (list workspaces/packages)
- **Languages & versions**: TODO(sdd)
- **Top-level layout**: TODO(sdd): key directories and what lives in each

## 2. Build, Lint & Test Commands (verified by running them)
| Purpose | Command | Runtime | Notes |
| :--- | :--- | :--- | :--- |
| Lint | TODO(sdd) | | |
| Build | TODO(sdd) | | |
| Unit tests | TODO(sdd) | | |
| Integration / E2E tests | TODO(sdd) | | prerequisites (Docker, DB, env vars) |

Copy the confirmed commands into `.specify/config.json` so `sdd_engine.py verify` runs exactly these.

## 3. Conventions Observed
- **Architecture / layering**: TODO(sdd)
- **Naming**: TODO(sdd)
- **Error handling**: TODO(sdd)
- **Dependency injection / configuration**: TODO(sdd)
- **Test style** (framework, fixtures, naming, where tests live): TODO(sdd)

## 4. Shared Contracts & Hot Spots
- **Shared libraries / contracts many modules depend on**: TODO(sdd)
- **Generated code (never edit by hand)**: TODO(sdd)
- **Known fragile or high-risk areas**: TODO(sdd)

## 5. Baseline Health
- **Current test status on the base branch**: TODO(sdd): pass / fail counts and known failing tests
