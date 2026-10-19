"""End-to-end tests for `verify` and `pr --dry-run` in throwaway git repositories."""

from __future__ import annotations

import shutil
import unittest

from toolkit_support import (SLUG, TempRootCase, capture, commit_all, config_json, engine, git, init_repo,
                             python_cmd)

MOD = f".specify/specs/{SLUG}"
UNITTEST_OK = python_cmd("print('Ran 3 tests in 0.01s'); print('OK')")


@unittest.skipUnless(shutil.which("git"), "git is required")
class GitRepoCase(TempRootCase):
    """A repo whose main branch has only setup files and whose feature branch holds a valid module."""

    test_cmd = UNITTEST_OK

    def setUp(self) -> None:
        super().setUp()
        init_repo(self.root)
        self.write("README.md", "# Fixture\n")
        self.write(".specify/config.json", config_json(self.test_cmd))
        commit_all(self.root, "chore: initial")
        git(self.root, "checkout", "-q", "-b", "feature/link-shortener")
        self.write_module()
        commit_all(self.root, "feat: link shortener")

    def run_cli(self, *args: str):
        return capture(engine.main, ["--root", str(self.root), *args])

    def verify(self):
        return self.run_cli("verify", "--module", SLUG)

    def verification(self) -> str:
        return (self.root / MOD / "verification.md").read_text(encoding="utf-8")


class VerifyTests(GitRepoCase):
    def test_pass_records_counts_and_fingerprint(self):
        rc, out = self.verify()
        self.assertEqual(rc, 0, out)
        report = self.verification()
        self.assertIn("RESULT: PASS", report)
        self.assertIn("- Passed: 3", report)
        fingerprint = engine.code_fingerprint(engine.Workspace(self.root))
        self.assertRegex(fingerprint, r"^[0-9a-f]{16}$")
        self.assertIn(f"FINGERPRINT: {fingerprint}", report)
        self.assertEqual(engine.verification_result(engine.Workspace(self.root), SLUG), ("PASS", fingerprint))
        self.assertTrue(any((self.root / ".specify" / ".runs" / SLUG).glob("*-test.log")))

    def test_nonzero_exit_fails(self):
        self.write(".specify/config.json", config_json(python_cmd("import sys; sys.exit(3)")))
        rc, out = self.verify()
        self.assertEqual(rc, 1, out)
        report = self.verification()
        self.assertIn("RESULT: FAIL", report)
        self.assertIn("test exited 3", report)

    def test_no_test_command_fails(self):
        self.write(".specify/config.json", config_json(None))
        rc, out = self.verify()
        self.assertEqual(rc, 1, out)
        report = self.verification()
        self.assertIn("RESULT: FAIL", report)
        self.assertIn("no test command configured", report)

    def test_traceability_errors_fail_even_when_tests_pass(self):
        self.write("tests/test_link.py", "def test_nothing():\n    pass\n")
        rc, out = self.verify()
        self.assertEqual(rc, 1, out)
        self.assertIn("traceability/hygiene error", self.verification())

    def test_baseline_never_fails(self):
        self.write(".specify/config.json", config_json(python_cmd("import sys; sys.exit(1)")))
        rc, _ = self.run_cli("verify", "--module", SLUG, "--baseline")
        self.assertEqual(rc, 0)
        self.assertIn("RESULT: FAIL", (self.root / MOD / "verification-baseline.md").read_text(encoding="utf-8"))

    def test_fingerprint_ignores_docs_and_specify_but_not_code(self):
        ws = engine.Workspace(self.root)
        before = engine.code_fingerprint(ws)
        self.write("docs/notes.md", "# notes\n")
        self.write(f"{MOD}/plan.md", "# Plan\n\nChanged.\n")
        self.assertEqual(engine.code_fingerprint(ws), before)
        self.write("src/link.py", "def shorten(url):\n    return url[-7:]\n")
        self.assertNotEqual(engine.code_fingerprint(ws), before)


class PrDryRunTests(GitRepoCase):
    def setUp(self) -> None:
        super().setUp()
        rc, out = self.verify()
        self.assertEqual(rc, 0, out)

    def pr(self):
        return self.run_cli("pr", "--module", SLUG, "--dry-run")

    def assertGate(self, fragment):
        rc, out = self.pr()
        self.assertEqual(rc, 1, out)
        gates = [line for line in out.splitlines() if "GATE FAIL" in line]
        self.assertTrue(any(fragment in g for g in gates), f"no gate containing {fragment!r}:\n{out}")
        return gates

    def test_all_gates_pass(self):
        rc, out = self.pr()
        self.assertEqual(rc, 0, out)
        self.assertIn("all gates passed", out)
        self.assertIn("Dry run", out)
        body = (self.root / MOD / "pr-body.md").read_text(encoding="utf-8")
        self.assertIn("Result: `PASS`", body)
        self.assertIn("tests/test_link.py", body)

    def test_review_not_approved(self):
        self.write(f"{MOD}/review-log.md", "## Iteration 1\nSTATUS: APPROVED\n\n## Iteration 2\nSTATUS: REJECTED\n")
        gates = self.assertGate("review-log.md final status is REJECTED")
        self.assertEqual(len(gates), 1, gates)

    def test_verification_missing(self):
        (self.root / MOD / "verification.md").unlink()
        gates = self.assertGate("verification.md result is NONE")
        self.assertEqual(len(gates), 1, gates)

    def test_fingerprint_mismatch_after_code_edit(self):
        self.write("src/link.py", "def shorten(url):\n    return url[-8:]\n")
        gates = self.assertGate("fingerprint mismatch")
        self.assertEqual(len(gates), 1, gates)

    def test_size_limit(self):
        self.write(".specify/config.json", config_json(UNITTEST_OK, max_pr_changed_lines=3))
        gates = self.assertGate("exceeds max_pr_changed_lines=3")
        self.assertEqual(len(gates), 1, gates)
        rc, out = self.run_cli("pr", "--module", SLUG, "--dry-run", "--allow-large")
        self.assertEqual(rc, 0, out)

    def test_secret_file_present(self):
        self.write(".env", "API_KEY=not-a-real-key\n")
        self.write(".env.example", "API_KEY=\n")
        self.verify()  # isolate the secret gate from the fingerprint gate
        gates = self.assertGate("likely secret files: ['.env']")
        self.assertEqual(len(gates), 1, gates)

    def test_force_overrides_and_records_gates(self):
        self.write(f"{MOD}/review-log.md", "STATUS: REJECTED\n")
        rc, out = self.run_cli("pr", "--module", SLUG, "--dry-run", "--force")
        self.assertEqual(rc, 0, out)
        body = (self.root / MOD / "pr-body.md").read_text(encoding="utf-8")
        self.assertIn("Gates overridden with --force", body)


if __name__ == "__main__":
    unittest.main()
