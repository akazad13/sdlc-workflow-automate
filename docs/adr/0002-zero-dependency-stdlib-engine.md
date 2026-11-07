# ADR-0002: Zero-Dependency Python 3 Standard Library Engine

## Status

Accepted

## Date

2026-10-20

## Context

The core orchestrator of this toolkit is the SDD engine (`scripts/sdd_engine.py`), which handles artifact scaffolding, bidirectional traceability validation, environment verification execution, and pull-request gate checks.

This toolkit is designed to be embedded into any software repository regardless of language stack (.NET, Node.js/TypeScript, Go, Rust, Python, Java, etc.) and run across varied environments:
- Developer local workstations (Windows, Linux, macOS)
- Ephemeral CI/CD runners (GitHub Actions, GitLab CI, Azure Pipelines)
- Sandboxed AI agent execution containers (Antigravity, Claude Code, Cursor, Devin)

Introducing external Python dependencies (e.g., `click`, `pydantic`, `pyyaml`, `requests`, `rich`) would require:
1. Setting up and managing virtual environments (`python -m venv .venv`).
2. Bootstrapping package managers (`pip`, `poetry`, `uv`) and dependency installations (`pip install -r requirements.txt`).
3. Dealing with potential version conflicts or environment collisions with the host project's existing tooling or global Python state.
4. Additional network calls, cache management, and failure points in CI merge gates.

## Decision

We will implement and maintain `scripts/sdd_engine.py` as a **zero-dependency script using strictly the Python 3 standard library** (Python 3.10+):

1. **Standard Library Modules Only**: All functionality (CLI parsing, process invocation, JSON handling, regex validation, HTTP requests, and unit testing) must rely exclusively on built-in modules (`argparse`, `dataclasses`, `json`, `re`, `subprocess`, `urllib.request`, `pathlib`, `unittest`, `hashlib`, `sys`, `os`).
2. **No `pip install` Required**: The engine must be immediately executable on any system with a standard Python 3.10+ runtime via `python scripts/sdd_engine.py <command>`.
3. **No External Dependencies for Companion Agents**: Auxiliary scripts such as `.github/scripts/pr_review_agent.py` also follow standard library rules (using `urllib.request` for Google Gemini API and GitHub REST API calls).
4. **Automated Enforcement**: Continuous integration and unit test suites (`tests/toolkit/`) verify that the toolkit functions cleanly without external third-party imports.

## Alternatives Considered

### Python with External Packages (Click / Typer, Pydantic, PyYAML, Requests)
- **Pros**: Cleaner CLI declarative models, robust YAML parsing out-of-the-box, rich terminal formatting, and convenient HTTP abstractions.
- **Cons**: Forces every host repository—including purely Go, Rust, or .NET projects—to configure Python virtual environments and install packages before running SDD validation gates. High friction for adoption and brittle in locked-down CI environments.
- **Rejected**: Drop-in portability and zero-setup execution outweigh the convenience of third-party helper libraries.

### Compiled Binary (Go or Rust)
- **Pros**: Self-contained single executable, fast execution, no Python runtime needed.
- **Cons**: Requires pre-compiled binaries distributed for every target OS and CPU architecture (Linux x86_64/arm64, Windows x64/arm64, macOS x64/arm64); binary blobs cannot be copied into repositories via plain text copy (`cp -r ... scripts/`); cannot be inspected, patched, or extended on the fly by AI coding agents.
- **Rejected**: Plain-text Python scripts are universally inspectable, portable across architectures, and transparent to both AI agents and humans.

### Shell Scripts (Bash / PowerShell)
- **Pros**: Native execution in command-line shells.
- **Cons**: High platform incompatibility (Bash vs PowerShell); difficult and fragile to parse complex multi-file Markdown templates, JSON configuration, and regex-based traceability graphs across OS boundaries.
- **Rejected**: Inadequate for complex AST/markdown parsing and cross-platform consistency.

## Consequences

- **Instant Portability**: Adopters can drop `scripts/sdd_engine.py` into any repository and immediately run `python scripts/sdd_engine.py validate --all`.
- **Zero CI Overhead**: CI workflows do not need `pip install` steps, reducing pipeline execution latency and eliminate supply-chain vulnerabilities from third-party packages.
- **Agent Friendly**: AI agents can run commands immediately in sandbox environments without dependency bootstrapping errors.
- **Design Constraints**:
  - Complex configuration and templates use JSON or lightweight markdown parsing rather than full YAML parsing.
  - HTTP integrations must manage network error handling and payload serialization with `urllib.request`.
  - Contributors must resist introducing third-party packages in `scripts/sdd_engine.py` and companion scripts.
