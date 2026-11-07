# ADR-0003: Testing Paradigm and Agentic Verification

## Status

Accepted

## Date

2026-11-07

## Context

The Autonomous Spec-Driven SDLC Toolkit relies heavily on autonomous AI coding agents (Antigravity, Claude Code, GitHub Copilot, Cursor, Devin) executing complex software development tasks: ingesting PRDs, authoring traceable specifications, breaking work into delivery tasks, writing code, and conducting code reviews.

In agentic workflows, traditional testing approaches face several distinct failure modes:
1. **Agent Self-Attestation & Hallucination**: AI agents frequently claim tasks are complete or that "all tests pass" without actually running the test commands, or they evaluate their own code with confirmation bias. Without independent, programmatic measurement, false positives easily slip into pull requests.
2. **Environment & Dependency Fragility**: Relying on third-party test runners (e.g., `pytest`, `pytest-cov`, `tox`, `mock`, `responses`) requires virtual environments (`.venv`), package managers (`pip`, `uv`), and network dependency installations. When agents run in locked-down sandboxes, ephemeral CI runners, or across heterogeneous host OS environments (Windows, Linux, macOS), dependency installation errors and version drift waste tokens, break workflows, and delay feedback loops.
3. **Traceability Gaps**: In typical codebases, tests are disconnected from requirements and acceptance criteria. When agents modify features or fix defects, it is impossible to deterministically verify whether all acceptance criteria remain covered or if tests were silently dropped or skipped.
4. **Slow Feedback Loops**: Complex integration tests and bloated fixture setups slow down test runs. AI agents require fast, deterministic, sub-second execution per unit test to sustain the rapid Red-Green-Refactor loop.

We need an explicit, architectural testing paradigm that governs how the toolkit tests itself and how agent tasks are objectively verified.

## Decision

We establish a **Zero-Dependency Python Standard Library Testing Paradigm** with **Objective Agentic Verification** and **Bidirectional Traceability**:

### 1. Pure Standard Library `unittest` Runner (Zero `pip install`)
- The toolkit test suite (`tests/toolkit/`) is implemented strictly using Python's built-in `unittest` framework and standard library modules (`unittest.mock`, `tempfile`, `pathlib`, `shutil`, `json`, `subprocess`, `io`, `sys`).
- No external packages or plugins (`pytest`, `pytest-cov`, `requests-mock`, `freezegun`) are permitted for toolkit testing.
- The test suite executes out-of-the-box on any system with Python 3.10+ via:
  ```bash
  python -m unittest discover -s tests/toolkit -p "test_*.py"
  ```
- Sub-second unit execution: Unit tests must execute in milliseconds without external network calls, keeping the entire 95+ test regression suite fast and reproducible (under 15 seconds even when spinning up ephemeral throwaway git repositories).

### 2. DAMP Test Design over DRY
- Test suites adhere to **DAMP** (Descriptive And Meaningful Phrases) rather than aggressive DRY (Don't Repeat Yourself).
- Each test case must be self-contained, clearly demonstrating its Arrange-Act-Assert structure, input data, and expected outcomes.
- Deep test inheritance hierarchies, complex shared fixture mutations, and obscure mocking helpers are prohibited. When a test fails, the agent or human engineer must immediately diagnose the root cause by reading the test itself without navigating layers of indirection.

### 3. The "Prove-It" Bug Reproduction Pattern
- For every bug fix, regression, or behavior modification:
  1. **RED**: Author a focused, failing unit test that reproduces the bug before altering any implementation code.
  2. **Verify Failure**: Execute the test and confirm it fails with the expected assertion error.
  3. **GREEN**: Implement the minimal production code change to make the test pass.
  4. **REFACTOR**: Clean up naming, structure, or performance while maintaining green tests.
- Pull requests fixing bugs without an accompanying reproduction test violate this invariant.

### 4. Independent, Objective Verification (No Self-Attestation)
- Autonomous agents are never permitted to self-certify their own work. Claims such as "tests pass" or "all criteria met" are inadmissible without empirical proof.
- The SDD engine (`scripts/sdd_engine.py verify`) acts as an external, objective measurement harness:
  - Invokes real test runner processes and captures OS exit codes.
  - Parses real test runner output to record explicit passed, failed, and skipped counts.
  - Detects and flags unauthorized test skips (e.g., `@unittest.skip`, `@pytest.mark.skip`, `it.skip`).
  - Computes a cryptographic SHA-256 code fingerprint of the working tree at verification time, ensuring code cannot be modified between verification and pull request generation.

### 5. Strict Bidirectional Traceability (`REQ` -> `AC` -> `TEST` -> `TASK`)
- Every PRD statement is assigned an immutable requirement identifier (`REQ-NNN`).
- Each requirement maps to granular Acceptance Criteria (`<KEY>-AC-NN`).
- Every acceptance criterion must be explicitly referenced by at least one test case (`<KEY>-E2E-NN` or test docstring) and mapped to an implementation task (`<KEY>-TNNN`).
- The engine's validation gate (`sdd_engine.py validate --all`) enforces this graph acyclically and deterministically. Missing test mappings or unassigned criteria immediately fail verification.

## Alternatives Considered

### Pytest and Third-Party Testing Ecosystem
- **Pros**: Concise `assert` syntax, powerful fixture injection, rich plugin ecosystem (e.g., `pytest-cov`, `pytest-xdist`).
- **Cons**: Requires creating virtual environments and running `pip install` before tests can execute. Fails in sandboxed agent containers or offline air-gapped CI environments. Introduces dependency version conflicts with the host repository's own Python environment.
- **Rejected**: Instant out-of-the-box execution across any environment via Python stdlib `unittest` is a non-negotiable architectural requirement (aligning directly with [ADR-0002](0002-zero-dependency-stdlib-engine.md)).

### Agent Self-Attested Verification
- **Pros**: Minimal tooling overhead; agents simply summarize what they tested in their response or PR description.
- **Cons**: High hallucination rate. Agents frequently assert that edge cases pass without running tests, or they assume mock responses succeed when actual implementations fail.
- **Rejected**: Independent process-level measurement and verification reports (`verification.md`) are required to ensure correctness and trust in autonomous agent outputs.

### Live External API Integration Testing in Unit Suites
- **Pros**: Tests real network interactions against GitHub REST API and Google Gemini API.
- **Cons**: Requires live API keys in test runners, incurs network latency, creates flaky tests due to rate limits or network hiccups, and prevents offline execution.
- **Rejected**: All unit tests in `tests/toolkit/` mock HTTP calls via standard library mechanisms (`urllib.request` mock wrappers) or use local throwaway git repositories, guaranteeing fast, hermetic, zero-cost test execution.

## Consequences

- **Zero-Friction Execution**: Any developer or autonomous agent can clone the repository and run the full 95+ unit test suite immediately without setting up virtual environments or installing dependencies.
- **Deterministic Agent Verification**: AI agents receive immediate, unambiguous feedback from test runs. Verification artifacts (`verification.md`, `.runs/`) provide tamper-proof evidence of correctness.
- **Bug Prevention**: The Prove-It pattern prevents regressions from reoccurring and ensures test suites expand organically with every defect fixed.
- **Clear Separation of Concerns**: The toolkit's own testing paradigm (100% stdlib `unittest`) remains independent from the target host projects it scaffolds (which may use .NET, TypeScript, Go, Rust, or Python pytest).
- **Design Trade-off**: Writing tests in stdlib `unittest` requires slightly more boilerplate (e.g., `self.assertEqual` instead of bare `assert`, `subTest` instead of `@pytest.mark.parametrize`), which is deliberately accepted to preserve zero-dependency portability and DAMP readability.
