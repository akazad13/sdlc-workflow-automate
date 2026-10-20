# Module Decomposition & Execution Roadmap

> Rows are derived from the PRD and `requirements.md`. Do not keep example rows.
> - `Key`: 2-6 uppercase letters, unique; prefixes all AC / E2E / task IDs for the module.
> - `Depends On`: comma-separated slugs or `None`. The graph must be acyclic (`sdd_engine.py validate --all`).
> - `Status`: `Planned`, `Specified`, `In Progress`, `In Review`, `Blocked`, `Done`.
> - Size: a module should be deliverable in about 1-5 PRs. Split anything larger.

| Phase | Module Name | Slug | Key | Depends On | Requirements | Primary Responsibilities | Status |
| :---: | :--- | :--- | :--- | :--- | :--- | :--- | :---: |
| 1 | TODO(sdd) | `todo-module` | TODO | None | REQ-001 | TODO(sdd) | Planned |

## Parallel Work Lanes
Modules whose dependencies are all `Done` may be specified and built in parallel by separate agents or teams.
Run `python scripts/sdd_engine.py status` to see which modules are unblocked.
