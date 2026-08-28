---
name: sdlc-reviewer
description: Automated PR code review, Roslyn static analysis checks, and security quality gating.
---

# SDLC Code Review & Quality Gate Skill

## Instructions
1. Review git diff and modified files against `csharp-standards.md` and `security-review.md`.
2. Compute Roslyn code quality metrics score (threshold $\ge 85\%$).
3. Output the formal review summary to `artifacts/pr_review_issue_{issueNumber}.md`.
4. Render decision (Approve / Request Changes).
