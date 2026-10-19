"""Tests for .github/scripts/pr_review_agent.py. Network calls are replaced with in-process fakes."""

from __future__ import annotations

import json
import os
import unittest
from unittest import mock

from toolkit_support import agent, capture

PATCH = "\n".join([
    "@@ -1,4 +1,5 @@ import os",
    " import os",
    "-x = 1",
    "+x = eval(input())",
    "+y = 2",
    " ",
    " print(x)",
    "@@ -20,2 +21,3 @@ def tail():",
    " a = 1",
    "+b = 2",
    " c = 3",
    "\\ No newline at end of file",
])


def file_diff(path="src/app.py", patch=PATCH):
    fd = agent.FileDiff(path, "modified", 3, 1, patch)
    fd.right_lines, fd.annotated_hunks = agent.annotate_patch(patch)
    return fd


def batch_of(*fds):
    return [(fd, "\n".join(fd.annotated_hunks) + "\n") for fd in fds]


class AnnotatePatchTests(unittest.TestCase):
    def test_numbers_added_and_context_lines_only(self):
        right, hunks = agent.annotate_patch(PATCH)
        self.assertEqual(right, {1, 2, 3, 4, 5, 21, 22, 23})
        self.assertEqual(len(hunks), 2)
        first = hunks[0].splitlines()
        self.assertEqual(first[0], "@@ -1,4 +1,5 @@ import os")
        self.assertEqual(first[1], "     1   import os")
        self.assertEqual(first[2], "       - x = 1")
        self.assertEqual(first[3], "     2 + x = eval(input())")
        self.assertEqual(first[4], "     3 + y = 2")
        self.assertEqual(first[5], "     4   ")
        self.assertEqual(first[6], "     5   print(x)")

    def test_no_newline_marker_is_not_a_line(self):
        patch = "@@ -1 +1 @@\n-old\n\\ No newline at end of file\n+new\n\\ No newline at end of file"
        right, hunks = agent.annotate_patch(patch)
        self.assertEqual(right, {1})
        self.assertNotIn("No newline", hunks[0])

    def test_trailing_newline_does_not_invent_a_line(self):
        right, hunks = agent.annotate_patch("@@ -1 +1 @@\n-a\n+b\n")
        self.assertEqual(right, {1})
        self.assertEqual(hunks, ["@@ -1 +1 @@\n       - a\n     1 + b"])

    def test_lines_before_first_hunk_are_ignored(self):
        right, hunks = agent.annotate_patch("diff --git a/x b/x\n+++ b/x\n@@ -0,0 +1 @@\n+hello")
        self.assertEqual((right, len(hunks)), ({1}, 1))


class BuildBatchesTests(unittest.TestCase):
    def make(self, path, n_lines, start=1):
        patch = f"@@ -0,0 +{start},{n_lines} @@\n" + "\n".join(f"+line {i:04d} of {path}" for i in range(n_lines))
        return file_diff(path, patch)

    def test_small_files_share_one_batch_in_order(self):
        files = [self.make(f"f{i}.py", 3) for i in range(3)]
        batches = agent.build_batches(files, 1000)
        self.assertEqual(len(batches), 1)
        self.assertEqual([f.path for f, _ in batches[0]], ["f0.py", "f1.py", "f2.py"])

    def test_files_spill_into_new_batches_under_the_limit(self):
        files = [self.make(f"f{i}.py", 8) for i in range(4)]
        size = len(files[0].annotated_hunks[0]) + 1
        batches = agent.build_batches(files, size * 2)
        self.assertEqual([[f.path for f, _ in b] for b in batches], [["f0.py", "f1.py"], ["f2.py", "f3.py"]])
        for b in batches:
            self.assertLessEqual(sum(len(c) for _, c in b), size * 2)

    def test_oversized_hunk_is_split_without_losing_lines(self):
        fd = self.make("big.py", 200)
        max_chars = 500
        batches = agent.build_batches([fd], max_chars)
        self.assertGreater(len(batches), 1)
        chunks = [c for b in batches for _, c in b]
        for c in chunks:
            self.assertLessEqual(len(c), max_chars)
        seen = [line for c in chunks for line in c.splitlines() if line != "(hunk continued)"]
        self.assertEqual(seen, fd.annotated_hunks[0].splitlines())

    def test_line_longer_than_limit_is_truncated_not_dropped(self):
        fd = file_diff("min.js", "@@ -0,0 +1,2 @@\n+" + "x" * 900 + "\n+tail")
        chunks = [c for b in agent.build_batches([fd], 300) for _, c in b]
        self.assertTrue(all(len(c) <= 300 for c in chunks))
        text = "".join(chunks)
        self.assertIn("     1 + xxx", text)
        self.assertIn("     2 + tail", text)


class ValidateFindingsTests(unittest.TestCase):
    def setUp(self):
        self.fd = file_diff()
        self.batch = batch_of(self.fd, file_diff(".github/workflows/ci.yml", "@@ -1 +1 @@\n-on: push\n+on: [push]"))

    def finding(self, **kw):
        base = {"file": "src/app.py", "start_line": 2, "end_line": 2, "severity": "Critical",
                "title": "eval of user input", "evidence": "x = eval(input())"}
        base.update(kw)
        return base

    def run_one(self, strict=True, **kw):
        out, _ = capture(agent.validate_findings, [self.finding(**kw)], self.batch, strict)
        return out

    def test_out_of_batch_file_is_dropped(self):
        self.assertEqual(self.run_one(file="src/elsewhere.py"), [])

    def test_dot_paths_are_kept_and_dot_slash_is_normalised(self):
        self.assertEqual(self.run_one(file=".github/workflows/ci.yml", start_line=1, end_line=1,
                                      evidence="on: [push]")[0]["file"], ".github/workflows/ci.yml")
        self.assertEqual(self.run_one(file="./src/app.py")[0]["file"], "src/app.py")

    def test_verified_blocking_finding_keeps_severity(self):
        f = self.run_one()[0]
        self.assertEqual((f["severity"], f["verified"], f["anchored"], f["note"]), ("Critical", True, True, ""))

    def test_unquotable_blocking_finding_is_downgraded(self):
        for sev in ("Critical", "Major"):
            with self.subTest(sev=sev):
                f = self.run_one(severity=sev, evidence="os.system(user_cmd)")[0]
                self.assertEqual(f["severity"], "Minor")
                self.assertFalse(f["verified"])
                self.assertIn(f"Downgraded from {sev}", f["note"])

    def test_non_strict_mode_keeps_unverified_severity(self):
        self.assertEqual(self.run_one(strict=False, evidence="nope")[0]["severity"], "Critical")

    def test_evidence_with_diff_and_line_number_prefixes_is_verified(self):
        for evidence in ("+x = eval(input())", "     2 + x = eval(input())", "2 + x = eval(input())",
                         "+x = eval(input())\n+y = 2", "  x  =  eval(input())  "):
            with self.subTest(evidence=evidence):
                f = self.run_one(evidence=evidence)[0]
                self.assertTrue(f["verified"])
                self.assertEqual(f["severity"], "Critical")

    def test_anchoring(self):
        self.assertTrue(self.run_one(start_line=21, end_line=23)[0]["anchored"])
        self.assertFalse(self.run_one(start_line=2, end_line=99)[0]["anchored"])
        self.assertFalse(self.run_one(start_line=0, end_line=0)[0]["anchored"])
        swapped = self.run_one(start_line=3, end_line=2)[0]
        self.assertEqual((swapped["start_line"], swapped["end_line"]), (2, 3))

    def test_severity_normalisation(self):
        self.assertEqual(self.run_one(severity="major")[0]["severity"], "Major")
        self.assertEqual(self.run_one(severity="blocker", evidence="zzz")[0]["severity"], "Minor")

    def test_non_dict_items_and_bad_lines_are_tolerated(self):
        out, _ = capture(agent.validate_findings, ["junk", self.finding(start_line="x")], self.batch, True)
        self.assertEqual(len(out), 1)
        self.assertEqual((out[0]["start_line"], out[0]["anchored"]), (0, False))


class VerdictAndParsingTests(unittest.TestCase):
    def test_verdict_for(self):
        self.assertEqual(agent.verdict_for([], False), "NO DEFECTS FOUND")
        self.assertEqual(agent.verdict_for([{"severity": "Minor"}], False), "APPROVED WITH SUGGESTIONS")
        self.assertEqual(agent.verdict_for([{"severity": "Nit"}], True), "INCOMPLETE REVIEW")
        self.assertEqual(agent.verdict_for([], True), "INCOMPLETE REVIEW")
        self.assertEqual(agent.verdict_for([{"severity": "Major"}], True), "CHANGES REQUESTED")
        self.assertEqual(agent.verdict_for([{"severity": "Critical"}], False), "CHANGES REQUESTED")

    def test_parse_model_json(self):
        payload = {"summary": "ok", "findings": []}
        for text in (json.dumps(payload), f"```json\n{json.dumps(payload)}\n```", f"```\n{json.dumps(payload)}\n```",
                     f"  Here you go:\n{json.dumps(payload)}\nThanks.  "):
            with self.subTest(text=text):
                self.assertEqual(agent.parse_model_json(text), payload)
        with self.assertRaises(json.JSONDecodeError):
            agent.parse_model_json("no json here")

    def test_dedupe_sorts_by_severity(self):
        f = lambda sev, line, title="t": {"file": "a.py", "start_line": line, "severity": sev, "title": title}  # noqa: E731
        out = agent.dedupe([f("Minor", 5), f("Critical", 9), f("Minor", 5, " T "), f("Major", 1)])
        self.assertEqual([(x["severity"], x["start_line"]) for x in out], [("Critical", 9), ("Major", 1), ("Minor", 5)])


# --------------------------------------------------------------------------------------------
# main() with fake GitHub and Gemini
# --------------------------------------------------------------------------------------------

HEAD = "abc1234def"
PR = {"title": "feat: app", "body": "Implements things.", "head": {"sha": HEAD}, "base": {"sha": "base000"}}
FILES = [{"filename": "src/app.py", "status": "modified", "additions": 3, "deletions": 1, "patch": PATCH},
         {"filename": "package-lock.json", "status": "modified", "additions": 900, "deletions": 0, "patch": "@@"}]


class FakeGitHub:
    def __init__(self, reviews=(), reject_inline=False):
        self.reviews, self.reject_inline, self.posts = list(reviews), reject_inline, []

    def request(self, path, method="GET", data=None, accept=None):
        if method == "POST":
            data = json.loads(json.dumps(data))  # snapshot: main() reuses and mutates the payload
            self.posts.append((path, data))
            if path.endswith("/reviews") and self.reject_inline and data.get("comments"):
                return 422, '{"message": "Unprocessable Entity: line must be part of the diff"}'
            return 200, "{}"
        if path == "/pulls/7":
            return 200, json.dumps(PR)
        if path.startswith("/pulls/7/reviews"):
            return 200, json.dumps(self.reviews)
        if path.startswith("/pulls/7/files"):
            return 200, json.dumps(FILES)
        return 404, '{"message": "Not Found"}'

    def review_posts(self):
        return [data for path, data in self.posts if path == "/pulls/7/reviews"]


def model_reply(*findings):
    return json.dumps({"summary": "Reviewed.", "strengths": [], "findings": list(findings)})


CRITICAL = {"file": "src/app.py", "start_line": 2, "end_line": 2, "severity": "Critical", "category": "security",
            "title": "eval of user input", "issue": "RCE", "evidence": "x = eval(input())", "suggested_fix": "int()"}
MINOR = dict(CRITICAL, severity="Minor", title="name y better", start_line=3, end_line=3, evidence="y = 2")


class MainTests(unittest.TestCase):
    ENV = {"GITHUB_TOKEN": "test-token", "GITHUB_REPOSITORY": "octo/repo", "PR_NUMBER": "7",
           "GEMINI_API_KEY": "test-key"}

    def run_main(self, fake, gemini_result=None, gemini_side_effect=None):
        gemini = mock.Mock(return_value=gemini_result, side_effect=gemini_side_effect)
        with mock.patch.dict(os.environ, self.ENV):
            for name in ("GITHUB_STEP_SUMMARY", "GEMINI_MODEL", "GEMINI_FALLBACK_MODELS", "AI_REVIEW_IGNORE",
                         "AI_REVIEW_BATCH_CHARS", "AI_REVIEW_MAX_BATCHES", "AI_REVIEW_STRICT_EVIDENCE",
                         "AI_REVIEW_FAIL_ON_PARTIAL", "AI_REVIEW_OPTIONAL"):
                os.environ.pop(name, None)
            with mock.patch.object(agent.GitHub, "request",
                                   lambda _self, path, method="GET", data=None, accept=None:
                                   fake.request(path, method, data, accept)), \
                    mock.patch.object(agent, "call_gemini", gemini):
                rc, out = capture(agent.main)
        return rc, out, gemini

    def test_changes_requested_exits_1_and_posts_inline_comment(self):
        fake = FakeGitHub()
        rc, out, gemini = self.run_main(fake, (model_reply(CRITICAL), "gemini-test"))
        self.assertEqual(rc, 1, out)
        gemini.assert_called_once()
        (review,) = fake.review_posts()
        self.assertEqual(review["commit_id"], HEAD)
        self.assertIn("CHANGES REQUESTED", review["body"])
        self.assertIn(f"sha={HEAD} verdict=CHANGES_REQUESTED", review["body"])
        self.assertIn("`package-lock.json`: ignored pattern", review["body"])
        self.assertEqual([(c["path"], c["line"], c["side"]) for c in review["comments"]], [("src/app.py", 2, "RIGHT")])

    def test_model_error_is_incomplete_review_and_exits_1(self):
        fake = FakeGitHub()
        rc, out, _ = self.run_main(fake, (None, "gemini-test: HTTP 500 boom"))
        self.assertEqual(rc, 1, out)
        (review,) = fake.review_posts()
        self.assertIn("INCOMPLETE REVIEW", review["body"])
        self.assertIn("HTTP 500 boom", review["body"])
        self.assertIn("`src/app.py`: model error", review["body"])

    def test_unparseable_model_output_is_incomplete(self):
        fake = FakeGitHub()
        rc, _, _ = self.run_main(fake, ("I think it looks fine!", "gemini-test"))
        self.assertEqual(rc, 1)
        self.assertIn("invalid JSON", fake.review_posts()[0]["body"])

    def test_clean_run_exits_0(self):
        fake = FakeGitHub()
        rc, out, _ = self.run_main(fake, (model_reply(), "gemini-test"))
        self.assertEqual(rc, 0, out)
        (review,) = fake.review_posts()
        self.assertIn("NO DEFECTS FOUND", review["body"])
        self.assertEqual(review["comments"], [])

    def test_inline_422_falls_back_to_body_only_review(self):
        fake = FakeGitHub(reject_inline=True)
        rc, out, _ = self.run_main(fake, (model_reply(MINOR), "gemini-test"))
        self.assertEqual(rc, 0, out)
        first, second = fake.review_posts()
        self.assertEqual(len(first["comments"]), 1)
        self.assertEqual(second["comments"], [])
        self.assertIn("Inline comments unavailable.", second["body"])
        self.assertIn("### Findings", second["body"])
        self.assertIn("name y better", second["body"])
        self.assertFalse([p for p, _ in fake.posts if p.startswith("/issues/")])

    def test_already_reviewed_head_is_skipped(self):
        for verdict, expected in (("NO_DEFECTS_FOUND", 0), ("CHANGES_REQUESTED", 1), ("INCOMPLETE_REVIEW", 1)):
            with self.subTest(verdict=verdict):
                prior = {"body": f"old review\n{agent.MARKER} sha={HEAD} verdict={verdict} -->"}
                fake = FakeGitHub(reviews=[{"body": "unrelated"}, prior])
                rc, out, gemini = self.run_main(fake, (model_reply(CRITICAL), "gemini-test"))
                self.assertEqual(rc, expected, out)
                gemini.assert_not_called()
                self.assertEqual(fake.posts, [])

    def test_review_of_older_sha_does_not_skip(self):
        prior = {"body": f"{agent.MARKER} sha=0000000 verdict=NO_DEFECTS_FOUND -->"}
        fake = FakeGitHub(reviews=[prior])
        rc, _, gemini = self.run_main(fake, (model_reply(), "gemini-test"))
        self.assertEqual(rc, 0)
        gemini.assert_called_once()

    def test_missing_api_key_fails_closed(self):
        with mock.patch.dict(os.environ, dict(self.ENV, GEMINI_API_KEY="", AI_REVIEW_OPTIONAL="")):
            rc, _ = capture(agent.main)
        self.assertEqual(rc, 1)
        with mock.patch.dict(os.environ, dict(self.ENV, GEMINI_API_KEY="", AI_REVIEW_OPTIONAL="true")):
            rc, _ = capture(agent.main)
        self.assertEqual(rc, 0)


if __name__ == "__main__":
    unittest.main()
