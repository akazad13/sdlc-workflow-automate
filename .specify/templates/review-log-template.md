# Review Log: [MODULE TITLE] (`[module-slug]`)

> Append one section per review iteration. Never edit earlier iterations.
> The last `STATUS:` line in this file is the module's review status and is read by `sdd_engine.py pr`.
> Allowed values: `STATUS: REJECTED`, `STATUS: APPROVED`, `STATUS: BLOCKED` (iteration budget exhausted or human decision needed).

## Baseline (before implementation)
- **Verification baseline**: `.specify/specs/[module-slug]/verification-baseline.md`
- **Pre-existing failures** (not caused by this module): None

---

## Iteration 1
- **Reviewer**: independent Review Agent (fresh context; did not write the code)
- **Commit / diff reviewed**: `<sha or "working tree">`
- **Verification**: `verification.md` result `<PASS|FAIL>` (tests passed / failed / skipped)

### Findings
| ID | Severity | Pillar | Location | Finding | Evidence | Resolution |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| R1-01 | Major | Spec Adherence | `src/path/file.ext:L10-L24` | [KEY]-AC-02 returns 500 instead of 400 | Test `[KEY]-AC-02 rejects empty email` fails | Open |

### AC Coverage Matrix
| AC / Scenario | Implemented In | Verified By (test) | Result |
| :--- | :--- | :--- | :--- |
| [KEY]-AC-01 | `src/...` | `tests/...::test_name` | Pass |

STATUS: REJECTED
