---
name: sdlc-ci-repair
description: Diagnostics and automated self-healing loop for broken builds or failing xUnit tests during .NET CI.
---

# SDLC CI Diagnostics & Self-Healing Skill

## Instructions
1. Run `dotnet test --verbosity normal`.
2. If tests fail, parse the xUnit assertion error, call stack, and line number.
3. Diagnose root cause (e.g. edge-case invariant, boundary condition, or race condition).
4. Apply minimal targeted fix in C# source files and re-execute `dotnet test` until all tests pass.
