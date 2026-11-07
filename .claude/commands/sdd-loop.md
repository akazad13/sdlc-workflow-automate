---
description: Run SDD loop — task-by-task TDD implementation with independent review and verification gate
disable-model-invocation: true
---

Invoke the sdd-loop skill.

User arguments: $ARGUMENTS

1. Establish baseline test state in verification-baseline.md.
2. Implement each task incrementally using red-green-refactor (TDD).
3. Conduct independent multi-axis review with file:line citations in review-log.md.
4. Run objective verification measurement with `python scripts/sdd_engine.py verify <module>`.
