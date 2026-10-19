"""validate_module / validate_roadmap tests for scripts/sdd_engine.py (no git involved)."""

from __future__ import annotations

import shutil
import unittest

from toolkit_support import (K, ONLY, SKIP, SLUG, TEMPLATES, TempRootCase, engine, roadmap_md, spec_md,
                             tasks_md, test_py)

MOD = f".specify/specs/{SLUG}"


class ValidateModuleTests(TempRootCase):
    def validate(self, stage="spec"):
        issues, coverage = engine.validate_module(self.ws(), SLUG, stage)
        return [i.message for i in issues if i.level == "ERROR"], [i.message for i in issues if i.level == "WARN"], coverage

    def assertError(self, errors, fragment):
        self.assertTrue(any(fragment in e for e in errors), f"no error containing {fragment!r} in {errors}")

    def test_valid_module_passes_both_stages(self):
        self.write_module()
        for stage in ("spec", "impl"):
            with self.subTest(stage=stage):
                errors, warns, _ = self.validate(stage)
                self.assertEqual(errors, [])
                self.assertEqual(warns, [])

    def test_missing_spec(self):
        errors, _, _ = self.validate()
        self.assertError(errors, "missing")

    def test_unresolved_markers_are_errors_but_note_lines_are_not(self):
        self.write_module()
        errors, _, _ = self.validate()
        self.assertEqual(errors, [])  # spec_md() has TODO(sdd) only in a "> " note line
        self.write(f"{MOD}/spec.md", spec_md() + "\n- **Security**: TODO(sdd): decide\n")
        self.write(f"{MOD}/plan.md", "# Plan\n\n[NEEDS CLARIFICATION: which queue?]\n")
        errors, _, _ = self.validate()
        marker_errors = [e for e in errors if "unresolved" in e]
        self.assertEqual(len(marker_errors), 2)
        self.assertTrue(any("spec.md has 1" in e for e in marker_errors))
        self.assertTrue(any("plan.md has 1" in e for e in marker_errors))

    def test_criterion_without_task(self):
        self.write_module(**{f"{MOD}/spec.md": spec_md() + f"- **{K}-AC-03 (Rate limit)** Traces: REQ-001\n"})
        errors, _, _ = self.validate()
        self.assertError(errors, f"criteria with no implementing task: ['{K}-AC-03']")

    def test_task_references_undefined_criterion(self):
        tasks = tasks_md() + f"- [x] {K}-T004 Extra (AC: {K}-AC-09) (Depends: none)\n"
        self.write_module(**{f"{MOD}/tasks.md": tasks})
        errors, _, _ = self.validate()
        self.assertError(errors, f"tasks.md references criteria not defined in spec.md: ['{K}-AC-09']")

    def test_unknown_task_dependency(self):
        tasks = tasks_md().replace(f"(Depends: {K}-T001)", f"(Depends: {K}-T099)")
        self.write_module(**{f"{MOD}/tasks.md": tasks})
        errors, _, _ = self.validate()
        self.assertError(errors, f"tasks depend on unknown task IDs: ['{K}-T099']")

    def test_task_dependency_cycle(self):
        tasks = tasks_md().replace("(Files: src/link.py) (Depends: none)", f"(Depends: {K}-T003)")
        self.write_module(**{f"{MOD}/tasks.md": tasks})
        errors, _, _ = self.validate()
        self.assertError(errors, "task dependency cycle")

    def test_duplicate_task_ids(self):
        self.write_module(**{f"{MOD}/tasks.md": tasks_md() + f"- [x] {K}-T001 Again (AC: {K}-AC-01)\n"})
        errors, _, _ = self.validate()
        self.assertError(errors, f"duplicate task IDs: ['{K}-T001']")

    def test_unknown_requirement(self):
        self.write_module(**{f"{MOD}/spec.md": spec_md() + "Also covers REQ-404.\n"})
        errors, _, _ = self.validate()
        self.assertError(errors, "requirements not in requirements.md: ['REQ-404']")

    def test_open_tasks_fail_impl_only(self):
        self.write_module(**{f"{MOD}/tasks.md": tasks_md(done=False)})
        self.assertEqual(self.validate("spec")[0], [])
        self.assertError(self.validate("impl")[0], "3 task(s) not complete")

    def test_coverage_accepts_hyphen_and_identifier_forms(self):
        self.write_module()
        errors, _, coverage = self.validate("impl")
        self.assertEqual(errors, [])
        self.assertEqual(coverage, {f"{K}-AC-01": ["tests/test_link.py"], f"{K}-AC-02": ["tests/test_link.py"],
                                    f"{K}-E2E-01": ["tests/test_link.py"]})

    def test_criteria_not_referenced_by_tests(self):
        body = test_py().replace(f"test_{K}_AC_02_rejects", "test_rejects").replace(f"# {K}-E2E-01", "")
        self.write_module(**{"tests/test_link.py": body + f"# {K}_AC_020 and X{K}-E2E-01 must not count\n"})
        errors, _, coverage = self.validate("impl")
        self.assertError(errors, f"criteria not referenced by any automated test: ['{K}-AC-02', '{K}-E2E-01']")
        self.assertEqual(coverage[f"{K}-AC-01"], ["tests/test_link.py"])

    def test_non_test_files_do_not_count_as_coverage(self):
        self.write_module(**{"tests/test_link.py": "def test_nothing():\n    pass\n",
                             "src/notes.py": f"# {K}-AC-01 {K}-AC-02 {K}-E2E-01\n"})
        self.assertError(self.validate("impl")[0], "criteria not referenced by any automated test")

    def test_focused_test_is_an_error(self):
        self.write_module(**{"tests/link.test.js": f"// {K}-AC-01\n{ONLY}'shortens', () => {{}});\n"})
        errors, _, _ = self.validate("impl")
        self.assertError(errors, "focused tests")
        self.assertError(errors, "tests/link.test.js:L2")

    def test_focused_test_in_file_using_identifier_form_is_found(self):
        self.write_module(**{"tests/link.test.js": f"describe('{K}_AC_01', () => {{\n  {ONLY}'x', () => {{}});\n}});\n"})
        self.assertError(self.validate("impl")[0], "tests/link.test.js:L2")

    def test_method_named_fit_is_not_a_focused_test(self):
        self.write_module(**{"tests/test_model.py": f"# {K}-AC-01\nmodel.fit(x, y)\nself.fit (x)\n"})
        self.assertEqual(self.validate("impl")[0], [])

    def test_skipped_test_is_a_warning(self):
        self.write_module(**{"tests/link.test.js": f"// {K}-AC-01\n{SKIP}'later', () => {{}});\n"})
        errors, warns, _ = self.validate("impl")
        self.assertEqual(errors, [])
        self.assertTrue(any("skipped/ignored tests" in w for w in warns), warns)


class ValidateRoadmapTests(TempRootCase):
    def validate_rows(self, rows):
        self.write(".specify/architecture/module-decomposition.md", roadmap_md("\n".join(rows)))
        ws = self.ws()
        return [i.message for i in engine.validate_roadmap(ws, engine.parse_roadmap(ws)) if i.level == "ERROR"]

    def assertError(self, errors, fragment):
        self.assertTrue(any(fragment in e for e in errors), f"no error containing {fragment!r} in {errors}")

    def test_missing_and_empty_roadmap(self):
        issues = engine.validate_roadmap(self.ws(), None)
        self.assertEqual([i.level for i in issues], ["WARN"])
        self.assertEqual(self.validate_rows([]), ["module-decomposition.md has no module table rows"])

    def test_valid_roadmap(self):
        self.assertEqual(self.validate_rows([
            "| 1 | Auth | `auth` | AU | None | REQ-001 | Planned |",
            "| 2 | Links | `links` | LN | `auth` | REQ-002 | Planned |",
        ]), [])

    def test_duplicate_keys_and_slugs(self):
        errors = self.validate_rows([
            "| 1 | Auth | `auth` | AU | None | | Planned |",
            "| 1 | Audit | `audit` | AU | None | | Planned |",
            "| 1 | Audit again | `audit` | AD | None | | Planned |",
        ])
        self.assertError(errors, "duplicate module keys: ['AU']")
        self.assertError(errors, "duplicate module slugs: ['audit']")

    def test_unknown_dependency(self):
        errors = self.validate_rows(["| 1 | Links | `links` | LN | auth, billing | | Planned |"])
        self.assertError(errors, "module 'links' depends on unknown modules ['auth', 'billing']")

    def test_dependency_cycle(self):
        errors = self.validate_rows([
            "| 1 | A | `a` | AA | c | | Planned |",
            "| 1 | B | `b` | BB | a | | Planned |",
            "| 1 | C | `c` | CC | b | | Planned |",
        ])
        self.assertError(errors, "module dependency cycle")

    def test_invalid_key(self):
        self.assertError(self.validate_rows(["| 1 | A | `a` | a1 | None | | Planned |"]), "invalid key 'a1'")

    def test_template_placeholder_row(self):
        dest = self.root / ".specify" / "architecture"
        dest.mkdir(parents=True)
        shutil.copy(TEMPLATES / "module-decomposition-template.md", dest / "module-decomposition.md")
        ws = self.ws()
        errors = [i.message for i in engine.validate_roadmap(ws, engine.parse_roadmap(ws)) if i.level == "ERROR"]
        self.assertEqual(errors, ["template placeholder row still present (slug 'todo-module')"])

    def test_done_module_without_approval_warns(self):
        self.write(".specify/architecture/module-decomposition.md",
                   roadmap_md("| 1 | A | `a` | AA | None | | Done |"))
        ws = self.ws()
        warns = [i.message for i in engine.validate_roadmap(ws, engine.parse_roadmap(ws)) if i.level == "WARN"]
        self.assertEqual(warns, ["module 'a' is Done but review-log is not APPROVED"])


if __name__ == "__main__":
    unittest.main()
