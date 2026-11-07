# ADR-0004: Cross-Agent Plugin Architecture & Packaging Standard

## Status

Accepted

## Date

2026-11-07

## Context

The Autonomous Spec-Driven SDLC Toolkit was originally maintained as a repository-level collection of `.agents/skills/`, a Python CLI engine (`scripts/sdd_engine.py`), and GitHub Action workflows.

With the standardization of the Agent Plugin Ecosystem (demonstrated by modern production agent plugins like `agent-skills`, Antigravity Customization System, Claude Code plugin marketplace, and the open skills CLI), autonomous software development workflows increasingly require unified packaging. Distributing workflows solely as repository folders forces consumers to manually copy directories across projects or script custom aliases, introducing version drift and installation friction.

We require a packaging architecture that transforms the SDLC Toolkit into a first-class, installable plugin across multiple agent runtimes while preserving the toolkit's core invariants: zero external dependencies in the CLI engine ([ADR-0002](0002-zero-dependency-stdlib-engine.md)) and pure standard library testing with objective verification ([ADR-0003](0003-testing-paradigm-and-agentic-verification.md)).

## Decision

We establish the **Cross-Agent Plugin Architecture & Packaging Standard** across the toolkit:

### 1. Canonical Plugin Manifest (`plugin.json`)
The repository root contains a standard-compliant `plugin.json` adhering to the `https://agent-plugins.org/schemas/1.0.0/plugin.schema.json` specification:
- Defines unique plugin identity (`sdlc-automate-workflow`), semantic version, descriptive metadata, repository pointers, and author information.
- Points directly to the bundled skills in `./.agents/skills`.
- Provides starter prompts for conversational agent invocation.

### 2. Multi-Platform Agent Manifests
To support heterogeneous agent runtimes without lock-in, the toolkit exports platform-specific configuration overlays:
- **Claude Code**: `.claude-plugin/plugin.json` exposing `./.agents/skills` and dedicated Markdown commands in `./.claude/commands/` with YAML frontmatter and `$ARGUMENTS` interpolation.
- **Antigravity / Gemini CLI**: `.gemini/commands/` providing declarative TOML slash commands mapping each lifecycle phase to its respective skill with `{{args}}` argument forwarding.
- **Agent Marketplace**: `.agents/plugins/marketplace.json` enabling discovery and one-click installation via `<plugin-card catalog-id="sdlc-automate-workflow"></plugin-card>`.
- **Open Skills CLI**: Compatible directory layout supporting `npx skills add akazad13/sdlc-automate-workflow`.

### 3. Declarative Slash Commands (`.gemini/commands/` and `.claude/commands/`)
Four core slash commands map directly to the four stages of Spec-Driven Development:
- `/sdd-intake`: Codebase discovery, PRD parsing into atomic `REQ-NNN` requirements, architecture interview, and module dependency graph generation.
- `/sdd-spec`: Traceable module specification (`spec.md`), technical plan (`plan.md`), and delivery tasks (`tasks.md`).
- `/sdd-loop`: Incremental Red-Green-Refactor implementation, independent evidence-based review (`review-log.md`), and objective test verification (`verification.md`).
- `/sdd-pr`: Pre-flight verification gates, README roadmap status synchronization, and right-sized or stacked pull request creation.

### 4. Preservation of Inviolable Engine Invariants & Plugin Fallbacks
Packaging the toolkit as an agent plugin does not compromise the underlying execution engine:
- The core CLI (`scripts/sdd_engine.py`) and review automation remain strictly Python 3 standard library (zero external dependencies).
- When invoked in a consumer repository where `.specify/templates/` is not yet local, `Workspace.render_template` seamlessly falls back to bundled engine templates without crashing.
- Target root defaults to `Path.cwd()` when executed globally or via plugin runners.
- Verification is always measured by real test runners and process exit codes, never by agent self-attestation.
- Tests remain executable via standard Python `unittest`.

## Alternatives Considered

### Loose Standalone Skills Only
- **Pros**: Minimal repository structure; no extra configuration files.
- **Cons**: Consumers cannot install or update the toolkit via plugin managers (`/plugin install`, `npx skills add`); requires manual copying of skill folders; lacks standardized command triggers.
- **Rejected**: Fails to provide an automated, installable developer experience.

### Merging Directly into Upstream `agent-skills`
- **Pros**: Instantly available to users of the `agent-skills` bundle.
- **Cons**: Conflates lightweight conversational engineering skills with a rigorous, stateful Python-driven SDD engine requiring local artifact templates, cryptographic verification fingerprints, and dedicated GitHub CI workflows.
- **Rejected**: The Autonomous Spec-Driven SDLC Toolkit operates as an independent, full-lifecycle verification system that synergizes with, but remains distinct from, general-purpose agent skill bundles.

## Consequences

- **Universal Multi-Agent Portability**: Developers can install the SDD toolkit across Antigravity, Claude Code, Cursor, and Codex with a single command.
- **Ergonomic Slash Commands**: Agents and humans can trigger structured workflows using `/sdd-intake`, `/sdd-spec`, `/sdd-loop`, and `/sdd-pr`.
- **Zero-Drift Distribution**: Updates to the toolkit propagate cleanly through plugin update mechanisms.
- **Strict Backward Compatibility**: Projects already using `scripts/sdd_engine.py` or `.specify/` templates continue operating without modification.
