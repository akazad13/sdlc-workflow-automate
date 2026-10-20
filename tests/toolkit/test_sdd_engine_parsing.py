"""Unit tests for the pure parsing helpers in scripts/sdd_engine.py."""

from __future__ import annotations

import shutil
import unittest

from toolkit_support import K, SLUG, TEMPLATES, TempRootCase, engine, roadmap_md


class DeriveKeyTests(unittest.TestCase):
    def test_single_word_slug_uses_word(self):
        self.assertEqual(engine.derive_key("billing"), "BILLIN")
        self.assertEqual(engine.derive_key("auth-service"), "AUTH")

    def test_multi_word_slug_uses_initials(self):
        self.assertEqual(engine.derive_key("user-profile-api"), "UP")
        self.assertEqual(engine.derive_key("order_line_items"), "OLI")
        self.assertEqual(engine.derive_key("a-b-c-d-e-f-g-h"), "ABCDEF")

    def test_all_generic_parts_fall_back_to_slug(self):
        self.assertEqual(engine.derive_key("core-api"), "COREAP")

    def test_leading_digit_and_short_keys_are_padded(self):
        self.assertEqual(engine.derive_key("9lives"), "M9LIVE")
        self.assertEqual(engine.derive_key("x"), "XX")
        self.assertEqual(engine.derive_key(""), "MX")

    def test_result_always_matches_key_rule(self):
        for slug in ("auth-service", "x", "9", "-", "123-abc", "user-2fa", "service", "a_b", "payments",
                     "very-long-module-name-with-many-parts", "api"):
            with self.subTest(slug=slug):
                self.assertRegex(engine.derive_key(slug), engine.KEY_RE)


class ParseTasksTests(unittest.TestCase):
    TEXT = f"""# Tasks
> Format: `- [ ] {K}-T001 Example (AC: {K}-AC-01) (Depends: none)`
- [ ] {K}-T001 [P] Domain (AC: {K}-AC-01, {K}-AC-02) (Files: a.py) (Depends: none)
- [x] {K}-T002 API (AC: {K}-AC-02) (Depends: {K}-T001)
  - [X] {K}-T003 E2E (AC: {K}-E2E-01, OTHER-AC-01) (Depends: {K}-T001, {K}-T002)
- [ ] Untracked chore without an id
- not a task line
"""

    def test_parses_ids_refs_deps_and_done(self):
        tasks = engine.parse_tasks(self.TEXT, K)
        self.assertEqual([t.id for t in tasks], [f"{K}-T001", f"{K}-T002", f"{K}-T003", None])
        self.assertEqual([t.done for t in tasks], [False, True, True, False])
        self.assertEqual(tasks[0].refs, [f"{K}-AC-01", f"{K}-AC-02"])
        self.assertEqual(tasks[0].depends, [])
        self.assertEqual(tasks[2].depends, [f"{K}-T001", f"{K}-T002"])

    def test_refs_are_limited_to_the_module_key(self):
        tasks = engine.parse_tasks(self.TEXT, K)
        self.assertEqual(tasks[2].refs, [f"{K}-E2E-01"])

    def test_note_lines_are_ignored_and_line_numbers_kept(self):
        tasks = engine.parse_tasks(self.TEXT, K)
        self.assertEqual([t.line_no for t in tasks], [3, 4, 5, 6])


class FindCycleTests(unittest.TestCase):
    def test_acyclic_graph(self):
        self.assertIsNone(engine.find_cycle({"a": ["b"], "b": ["c"], "c": [], "d": ["a", "c"]}))

    def test_reports_cycle_path(self):
        cycle = engine.find_cycle({"a": ["b"], "b": ["c"], "c": ["a"]})
        self.assertEqual(cycle[0], cycle[-1])
        self.assertEqual(set(cycle), {"a", "b", "c"})
        self.assertEqual(len(cycle), 4)

    def test_self_loop(self):
        self.assertEqual(engine.find_cycle({"a": ["a"]}), ["a", "a"])

    def test_cycle_not_reachable_from_first_node(self):
        cycle = engine.find_cycle({"root": [], "x": ["y"], "y": ["x"]})
        self.assertEqual(set(cycle), {"x", "y"})

    def test_unknown_nodes_are_ignored(self):
        self.assertIsNone(engine.find_cycle({"a": ["missing"], "b": ["a"]}))

    def test_deep_chain_does_not_hit_recursion_limit(self):
        n = 5000
        graph = {f"n{i}": [f"n{i + 1}"] for i in range(n)}
        graph[f"n{n}"] = []
        self.assertIsNone(engine.find_cycle(graph))
        graph[f"n{n}"] = ["n0"]
        self.assertEqual(len(engine.find_cycle(graph)), n + 2)


class RequirementCandidateTests(unittest.TestCase):
    def test_extracts_modal_bullets_with_nearest_heading(self):
        prd = """# Product
Intro text that must be ignored because it is not a bullet.
- Users must be able to sign up with email
## Payments
1. The system shall refund within 5 days
2) Admins can export invoices | as CSV
* short must
- A plain bullet with no modal verb here
###
+ Reports should support filtering by date
"""
        self.assertEqual(engine.extract_requirement_candidates(prd), [
            ("Users must be able to sign up with email", "Product"),
            ("The system shall refund within 5 days", "Payments"),
            ("Admins can export invoices / as CSV", "Payments"),
            ("Reports should support filtering by date", "Payments"),
        ])

    def test_empty_prd(self):
        self.assertEqual(engine.extract_requirement_candidates(""), [])


class ParseRoadmapTests(TempRootCase):
    def test_missing_file_returns_none(self):
        self.assertIsNone(engine.parse_roadmap(self.ws()))

    def test_parses_rows_and_backticked_dependencies(self):
        rows = "\n".join([
            f"| 1 | Link Shortener | `{SLUG}` | {K} | None | REQ-001, REQ-002 | Planned |",
            "| 2 | Analytics | `analytics` | AN | `link-shortener`, `auth` | REQ-003 | Done |",
            "| 2 | Auth | auth | AU | - | | In Progress |",
        ])
        self.write(".specify/architecture/module-decomposition.md", roadmap_md(rows) + "\n| other | table |\n")
        roadmap = engine.parse_roadmap(self.ws())
        self.assertEqual([r.slug for r in roadmap], [SLUG, "analytics", "auth"])
        first, second, third = roadmap
        self.assertEqual((first.key, first.title, first.phase, first.status), (K, "Link Shortener", "1", "Planned"))
        self.assertEqual(first.requirements, ["REQ-001", "REQ-002"])
        self.assertEqual(first.depends, [])
        self.assertEqual(second.depends, [SLUG, "auth"])
        self.assertEqual(third.depends, [])

    def test_template_parses_to_placeholder_row(self):
        dest = self.root / ".specify" / "architecture"
        dest.mkdir(parents=True)
        shutil.copy(TEMPLATES / "module-decomposition-template.md", dest / "module-decomposition.md")
        roadmap = engine.parse_roadmap(self.ws())
        self.assertEqual(len(roadmap), 1)
        self.assertEqual((roadmap[0].slug, roadmap[0].key), ("todo-module", "TODO"))


class ParseTestCountsTests(unittest.TestCase):
    def assertCounts(self, output, passed, failed, skipped):
        self.assertEqual(engine.parse_test_counts(output),
                         {"passed": passed, "failed": failed, "skipped": skipped})

    def test_pytest(self):
        out = ("============================= test session starts ==============================\n"
               "collected 13 items\n\ntests/test_a.py ..F.ss\n"
               "=========================== short test summary info ============================\n"
               "FAILED tests/test_a.py::test_x - assert 1 == 2\n"
               "=================== 1 failed, 10 passed, 2 skipped in 0.12s ====================\n")
        self.assertCounts(out, 10, 1, 2)

    def test_pytest_quiet(self):
        self.assertCounts("..F\nFAILED tests/test_a.py::test_x\n1 failed, 10 passed, 3 warnings in 0.50s\n", 10, 1, None)
        self.assertCounts("....\n4 passed in 0.01s\n", 4, None, None)

    def test_jest(self):
        out = ("Test Suites: 1 failed, 2 passed, 3 total\n"
               "Tests:       1 failed, 2 skipped, 10 passed, 13 total\nSnapshots:   0 total\n")
        self.assertCounts(out, 10, 1, 2)

    def test_vitest(self):
        out = (" Test Files  1 failed | 1 passed (2)\n"
               "      Tests  1 failed | 10 passed | 2 skipped (13)\n   Duration  1.2s\n")
        self.assertCounts(out, 10, 1, 2)

    def test_dotnet_classic_logger_sums_test_projects(self):
        out = ("Passed!  - Failed:     0, Passed:    12, Skipped:     1, Total:    13, Duration: 1 s - A.Tests.dll\n"
               "Failed!  - Failed:     2, Passed:     5, Skipped:     0, Total:     7, Duration: 1 s - B.Tests.dll\n")
        self.assertCounts(out, 17, 2, 1)

    def test_dotnet_summary_line(self):
        out = ("A.Tests test succeeded (1.2s)\n"
               "Test summary: total: 13, failed: 1, succeeded: 11, skipped: 1, duration: 2.1s\n")
        self.assertCounts(out, 11, 1, 1)

    def test_cargo_sums_test_binaries(self):
        out = ("running 4 tests\ntest a ... ok\n"
               "test result: ok. 3 passed; 0 failed; 1 ignored; 0 measured; 0 filtered out; finished in 0.00s\n"
               "running 3 tests\n"
               "test result: FAILED. 2 passed; 1 failed; 0 ignored; 0 measured; 0 filtered out; finished in 0.01s\n")
        self.assertCounts(out, 5, 1, 1)

    def test_go_verbose(self):
        out = ("=== RUN   TestA\n--- PASS: TestA (0.00s)\n=== RUN   TestB\n    --- PASS: TestB/sub (0.00s)\n"
               "--- FAIL: TestB (0.00s)\n=== RUN   TestC\n--- SKIP: TestC (0.00s)\n"
               "=== RUN   TestD\n--- PASS: TestD (0.00s)\nFAIL\nFAIL\texample.com/pkg\t0.01s\n")
        self.assertCounts(out, 2, 1, 1)

    def test_python_unittest(self):
        out = ("test_a (t.T.test_a) ... ok\n\n"
               "----------------------------------------------------------------------\n"
               "Ran 6 tests in 0.010s\n\nFAILED (failures=1, errors=1, skipped=1)\n")
        self.assertCounts(out, 3, 2, 1)
        self.assertCounts("Ran 1 test in 0.001s\n\nOK\n", 1, 0, 0)
        self.assertCounts("Ran 4 tests in 0.001s\n\nOK (skipped=1, expected failures=1)\n", 3, 0, 1)

    def test_unknown_output(self):
        self.assertCounts("all good\n", None, None, None)


if __name__ == "__main__":
    unittest.main()
