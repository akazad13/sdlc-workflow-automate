---
name: "sdd-pr"
description: "Enforce pre-flight gates, sync the README roadmap, open right-sized (optionally stacked) GitHub pull requests, and triage CI and AI review feedback. Merging stays a human decision."
compatibility: "Requires spec-kit project structure with .specify/ directory, Python 3.10+, git, and gh"
---

# SDD PR: Gated, Right-Sized Pull Requests

You are the Release Engineer. Your job is to get verified work in front of human reviewers in a
form they can actually review, and to respond to feedback accurately.

## User Input

```text
$ARGUMENTS
```

The module slug. If omitted, use the module with status `In Review` in `python scripts/sdd_engine.py status`.

---

## Step 1: Pre-Flight

```bash
python scripts/sdd_engine.py pr --module <slug> --dry-run
```

The engine refuses to continue unless all gates pass:

| Gate | Source |
| :--- | :--- |
| Review approved | Last `STATUS:` line in `review-log.md` is `APPROVED` |
| Verification passed | `verification.md` has `RESULT: PASS` |
| Verification is current | Code fingerprint matches the code now (rerun `verify` after any code change) |
| Traceability | Every AC/E2E ID referenced by a test, all tasks ticked, no focused tests |
| Size | Changed lines (excluding docs, `.specify/`, lockfiles) within `max_pr_changed_lines` |
| Hygiene | No likely secret files (`.env`, keys, certificates) about to be committed |

Fix the cause of a failed gate. Use `--force` only when the user explicitly accepts the risk; the
override is written into the PR body.

## Step 2: Sync Documentation

Update `README.md` (Markdown changes do not invalidate verification):
- Module roadmap row: branch, PR link (after Step 3), review gate, test counts copied from `verification.md`.
- Project structure and documentation links for new directories and the module's spec and review log.

Only report numbers that appear in `verification.md`. Do not round up or estimate coverage.

## Step 3: Open the PR

**Normal size:**
```bash
python scripts/sdd_engine.py pr --module <slug>
```
This switches to `feature/<slug>` if you are on the base branch, commits, pushes, and opens the PR
with a generated body (`.specify/specs/<slug>/pr-body.md`) listing requirements, the AC-to-test
mapping, review iterations, and measured results. If a PR for the branch exists, the push updates it.

**Too large (size gate):** ship the delivery slices from `plan.md` as stacked PRs:
1. Slice 1 on `feature/<slug>-1` against `main`.
2. Slice 2: `git switch -c feature/<slug>-2 feature/<slug>-1`, implement and verify it, then `python scripts/sdd_engine.py pr --module <slug> --base feature/<slug>-1` (the engine keeps the current branch when it differs from the base).
3. Each slice must build and pass tests on its own. Merge bottom-up; after a lower slice merges, rebase the next onto `main` and retarget it.

Use `--draft` while CI is expected to need fixes.

## Step 4: CI and AI Review Triage

```bash
gh pr checks <pr-number> --watch
gh pr view <pr-number> --comments
```

The AI reviewer posts one review per commit with inline findings and fails its check on Critical or
Major findings, or when it could not review everything it was asked to (`INCOMPLETE REVIEW`).

For each finding:
1. Check it against the code. AI reviewers produce false positives; findings marked "Downgraded" had
   evidence the reviewer could not quote from the diff.
2. Real defect: fix it on the branch, rerun `python scripts/sdd_engine.py verify --module <slug>`, push.
3. False positive: reply on the thread with the reason and evidence (file:line, test name). Do not
   change correct code to satisfy a wrong comment.
4. Never disable, skip, or bypass a failing check.

## Step 5: Merge (human decision)

Do not merge autonomously. A merge requires: all required checks green, no unresolved Critical or
Major AI findings, and at least one human approval (configure branch protection and CODEOWNERS).
When the user asks you to merge after those conditions hold:

```bash
gh pr merge <pr-number> --squash --delete-branch
git switch main && git pull --ff-only
```

Then set the module's roadmap status to `Done` and run `python scripts/sdd_engine.py status` to show
which modules are now unblocked and can proceed in parallel.
