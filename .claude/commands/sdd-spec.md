---
description: Run SDD spec — generate traceable module spec (REQ -> AC -> E2E -> tasks), technical plan, and task list
disable-model-invocation: true
---

Invoke the sdd-spec skill.

User arguments: $ARGUMENTS

1. Generate spec.md with bidirectional REQ -> AC -> E2E traceability.
2. Produce plan.md with delivery slices and project context alignment.
3. Scaffold tasks.md with atomic, ordered, test-driven implementation steps.
4. Validate artifacts with `python scripts/sdd_engine.py validate <module> --stage spec`.
