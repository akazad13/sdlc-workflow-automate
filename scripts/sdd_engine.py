#!/usr/bin/env python3
"""
SDD Engine: deterministic scaffolding and verification for the Spec-Driven Development workflow.

The engine does not do the reasoning work and never reports a result it did not measure.
Agent skills (.agents/skills/sdd-*) write requirements, specs, code, tests, and reviews. This CLI
scaffolds artifacts from .specify/templates and then independently checks them:

  intake    Scaffold requirements register, architecture, roadmap, and project context.
  spec      Scaffold a module spec suite (spec, plan, tasks, checklist) from templates.
  validate  Check IDs, traceability (REQ -> AC -> task -> test), markers, and roadmap DAG.
  verify    Run the real lint/build/test commands and record results in verification.md.
  status    Show the roadmap with each module's readiness and gate state.
  pr        Enforce pre-flight gates, then branch, commit, push, and open the PR (never merges).
  loop      Simulated generator/reviewer loop for demos (--mock only).
  run-all   Smoke test: intake -> spec -> validate -> (mock loop) -> pr --dry-run.

Zero third-party dependencies. Python 3.10+.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import os
import re
import shutil
import subprocess
import sys
import time
from dataclasses import dataclass, field
from datetime import datetime
from pathlib import Path
from typing import Dict, Iterable, List, Optional, Tuple

DEFAULT_ROOT = Path(__file__).resolve().parent.parent

DEFAULT_CONFIG = {
    "base_branch": "main",
    "branch_prefix": "feature/",
    "commands": {"lint": None, "build": None, "test": None},
    "module_commands": {},
    "test_path_markers": ["test", "tests", "spec", "specs", "e2e", "__tests__"],
    "ignore_dirs": [".git", ".specify", "node_modules", "bin", "obj", "dist", "build", "target",
                    ".venv", "venv", "vendor", "coverage", "TestResults"],
    "max_pr_changed_lines": 600,
    "max_review_iterations": 3,
    "command_timeout_seconds": 1800,
}

CODE_EXTENSIONS = {
    ".py", ".js", ".jsx", ".ts", ".tsx", ".mjs", ".cjs", ".cs", ".fs", ".vb", ".go", ".rs", ".java",
    ".kt", ".kts", ".scala", ".rb", ".php", ".swift", ".feature", ".robot", ".dart", ".ex", ".exs",
}
LOCKFILE_NAMES = {"package-lock.json", "yarn.lock", "pnpm-lock.yaml", "poetry.lock", "Cargo.lock",
                  "go.sum", "packages.lock.json", "composer.lock", "Gemfile.lock"}
SECRET_FILE_PATTERNS = [r"(^|/)\.env(?!\.(example|sample|template|dist)$)(\.|$)", r"\.pem$", r"\.pfx$", r"\.p12$", r"\.key$",
                        r"(^|/)id_(rsa|ed25519|ecdsa)$", r"\.secrets?\.json$", r"(^|/)credentials\.json$"]
# Jasmine/Jest globals only: `model.fit(` or `self.fit(` is a method call, not a focused test.
FOCUSED_TEST_PATTERNS = [r"\b(it|describe|test)\.only\s*\(", r"(?<![\w.$])fit\s*\(", r"(?<![\w.$])fdescribe\s*\("]
SKIPPED_TEST_PATTERNS = [r"\b(it|describe|test)\.skip\s*\(", r"(?<![\w.$])xit\s*\(", r"(?<![\w.$])xdescribe\s*\(",
                         r"@pytest\.mark\.skip\b", r"@unittest\.skip\b", r"\bSkip\s*=\s*\"",
                         r"\[Ignore\b", r"\bt\.Skip\(", r"#\[ignore\]", r"@Disabled\b"]

KEY_RE = re.compile(r"^[A-Z][A-Z0-9]{1,5}$")
REQ_RE = re.compile(r"\bREQ-\d{3,}\b")
MARKER_RE = re.compile(r"TODO\(sdd\)|\[NEEDS CLARIFICATION")
STATUS_RE = re.compile(r"^STATUS:\s*([A-Z]+)\b", re.MULTILINE)
RESULT_RE = re.compile(r"^RESULT:\s*(PASS|FAIL)\b", re.MULTILINE)
FINGERPRINT_RE = re.compile(r"^FINGERPRINT:\s*(\S+)", re.MULTILINE)


# --------------------------------------------------------------------------------------------
# Output helpers
# --------------------------------------------------------------------------------------------

def say(msg: str = "") -> None:
    print(msg, flush=True)


def heading(title: str) -> None:
    say(f"\n=== {title} ===")


def print_table(headers: List[str], rows: List[List[str]]) -> None:
    widths = [len(h) for h in headers]
    for row in rows:
        for i, cell in enumerate(row):
            widths[i] = max(widths[i], len(str(cell)))
    fmt = "  ".join("{:<" + str(w) + "}" for w in widths)
    say(fmt.format(*headers))
    say(fmt.format(*["-" * w for w in widths]))
    for row in rows:
        say(fmt.format(*[str(c) for c in row]))


# --------------------------------------------------------------------------------------------
# Workspace, config, and artifact helpers
# --------------------------------------------------------------------------------------------

class Workspace:
    def __init__(self, root: Path):
        self.root = root
        self.specify = root / ".specify"
        self.arch = self.specify / "architecture"
        self.specs = self.specify / "specs"
        self.templates = self.specify / "templates"
        self.runs = self.specify / ".runs"
        self.config = self._load_config()
        self._test_corpus: Optional[Dict[str, str]] = None

    def _load_config(self) -> dict:
        cfg = json.loads(json.dumps(DEFAULT_CONFIG))
        path = self.specify / "config.json"
        if path.exists():
            try:
                user = json.loads(path.read_text(encoding="utf-8"))
            except json.JSONDecodeError as exc:
                raise SystemExit(f"error: {path} is not valid JSON: {exc}")
            for k, v in user.items():
                if isinstance(v, dict) and isinstance(cfg.get(k), dict):
                    cfg[k].update(v)
                else:
                    cfg[k] = v
        return cfg

    def module_dir(self, slug: str) -> Path:
        return self.specs / slug

    def rel(self, path: Path) -> str:
        try:
            return path.relative_to(self.root).as_posix()
        except ValueError:
            return str(path)

    def render_template(self, name: str, slug: str = "", title: str = "", key: str = "") -> str:
        path = self.templates / name
        if not path.exists():
            raise SystemExit(f"error: template not found: {self.rel(path)}")
        text = path.read_text(encoding="utf-8")
        return (text.replace("[MODULE TITLE]", title)
                    .replace("[module-slug]", slug)
                    .replace("[KEY]", key))

    def write_artifact(self, path: Path, content: str, force: bool) -> bool:
        if path.exists() and not force:
            say(f"  kept     {self.rel(path)} (exists; use --force to overwrite)")
            return False
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(content, encoding="utf-8")
        say(f"  wrote    {self.rel(path)}")
        return True

    def test_corpus(self) -> Dict[str, str]:
        """All test-like source files (path -> text). Cached for the run."""
        if self._test_corpus is not None:
            return self._test_corpus
        markers = {m.lower() for m in self.config["test_path_markers"]}
        ignore = set(self.config["ignore_dirs"])
        corpus: Dict[str, str] = {}
        for dirpath, dirnames, filenames in os.walk(self.root):
            dirnames[:] = [d for d in dirnames if d not in ignore and not d.startswith(".")]
            rel_dir = Path(dirpath).relative_to(self.root)
            in_test_dir = any(part.lower() in markers for part in rel_dir.parts)
            for name in filenames:
                p = Path(dirpath) / name
                if p.suffix.lower() not in CODE_EXTENSIONS:
                    continue
                lower = name.lower()
                if not (in_test_dir or "test" in lower or ".spec." in lower or "_spec" in lower):
                    continue
                try:
                    if p.stat().st_size > 2_000_000:
                        continue
                    corpus[self.rel(p)] = p.read_text(encoding="utf-8", errors="ignore")
                except OSError:
                    continue
        self._test_corpus = corpus
        return corpus


def derive_key(slug: str) -> str:
    generic = {"service", "module", "api", "core", "app", "svc"}
    parts = [p for p in re.split(r"[-_]", slug) if p and p.lower() not in generic] or [slug]
    if len(parts) == 1:
        key = re.sub(r"[^A-Za-z0-9]", "", parts[0])[:6].upper()
    else:
        key = "".join(p[0] for p in parts)[:6].upper()
    if not key or not key[0].isalpha():
        key = "M" + key
    return key[:6] if len(key) >= 2 else (key + "X")


def read_text(path: Path) -> str:
    return path.read_text(encoding="utf-8", errors="ignore") if path.exists() else ""


def last_match(regex: re.Pattern, text: str) -> Optional[str]:
    found = regex.findall(text)
    return found[-1] if found else None


# --------------------------------------------------------------------------------------------
# Roadmap / spec / task parsing
# --------------------------------------------------------------------------------------------

@dataclass
class ModuleRow:
    slug: str
    key: str = ""
    title: str = ""
    depends: List[str] = field(default_factory=list)
    status: str = ""
    phase: str = ""
    requirements: List[str] = field(default_factory=list)


def _cells(line: str) -> List[str]:
    return [c.strip().strip("`").strip() for c in line.strip().strip("|").split("|")]


def parse_roadmap(ws: Workspace) -> Optional[List[ModuleRow]]:
    path = ws.arch / "module-decomposition.md"
    if not path.exists():
        return None
    lines = path.read_text(encoding="utf-8").splitlines()
    rows: List[ModuleRow] = []
    i = 0
    while i < len(lines):
        line = lines[i]
        if line.strip().startswith("|") and "slug" in line.lower():
            headers = [h.lower() for h in _cells(line)]
            i += 2  # skip header separator
            while i < len(lines) and lines[i].strip().startswith("|"):
                cells = _cells(lines[i])
                rec = dict(zip(headers, cells))

                def col(*names: str) -> str:
                    for n in names:
                        for h, v in rec.items():
                            if n in h:
                                return v
                    return ""

                deps_raw = col("depends", "dependencies")
                deps = [] if deps_raw.lower() in ("", "none", "-", "n/a") else \
                    [d.strip().strip("`") for d in deps_raw.split(",") if d.strip()]
                rows.append(ModuleRow(
                    slug=col("slug"), key=col("key"), title=col("module name", "module"),
                    depends=deps, status=col("status"), phase=col("phase"),
                    requirements=REQ_RE.findall(col("requirements")),
                ))
                i += 1
            break
        i += 1
    return rows


def module_key(ws: Workspace, slug: str, roadmap: Optional[List[ModuleRow]] = None) -> str:
    roadmap = roadmap if roadmap is not None else (parse_roadmap(ws) or [])
    for row in roadmap:
        if row.slug == slug and KEY_RE.match(row.key or ""):
            return row.key
    spec = read_text(ws.module_dir(slug) / "spec.md")
    m = re.search(r"\*\*Module Key:\*\*\s*`([A-Z][A-Z0-9]{1,5})`", spec)
    return m.group(1) if m else derive_key(slug)


@dataclass
class Task:
    id: Optional[str]
    done: bool
    refs: List[str]
    depends: List[str]
    line_no: int


TASK_LINE_RE = re.compile(r"^\s*-\s*\[( |x|X)\]\s*(?P<id>[A-Z][A-Z0-9]*-T\d+)?(?P<rest>.*)$")
TASK_ID_RE = re.compile(r"\b[A-Z][A-Z0-9]*-T\d+\b")


def parse_tasks(text: str, key: str) -> List[Task]:
    ref_re = re.compile(rf"\b{re.escape(key)}-(?:AC|E2E)-\d+\b")
    tasks = []
    for n, line in enumerate(text.splitlines(), 1):
        if line.lstrip().startswith(">"):
            continue
        m = TASK_LINE_RE.match(line)
        if not m:
            continue
        rest = m.group("rest")
        dep_m = re.search(r"Depends:\s*([^)]*)", rest)
        depends = TASK_ID_RE.findall(dep_m.group(1)) if dep_m else []
        tasks.append(Task(id=m.group("id"), done=m.group(1).lower() == "x",
                          refs=sorted(set(ref_re.findall(rest))), depends=depends, line_no=n))
    return tasks


def find_cycle(graph: Dict[str, List[str]]) -> Optional[List[str]]:
    """Return one cycle as [a, b, ..., a], or None. Iterative DFS, so long dependency chains are safe."""
    WHITE, GREY, BLACK = 0, 1, 2
    color = {n: WHITE for n in graph}
    done = object()
    for root in graph:
        if color[root] != WHITE:
            continue
        color[root] = GREY
        path, pending = [root], [iter(graph.get(root) or [])]
        while pending:
            nxt = next(pending[-1], done)
            if nxt is done:
                color[path.pop()] = BLACK
                pending.pop()
            elif nxt not in color:
                continue
            elif color[nxt] == GREY:
                return path[path.index(nxt):] + [nxt]
            elif color[nxt] == WHITE:
                color[nxt] = GREY
                path.append(nxt)
                pending.append(iter(graph.get(nxt) or []))
    return None


def review_status(ws: Workspace, slug: str) -> str:
    return last_match(STATUS_RE, read_text(ws.module_dir(slug) / "review-log.md")) or "NONE"


def verification_result(ws: Workspace, slug: str) -> Tuple[str, Optional[str]]:
    text = read_text(ws.module_dir(slug) / "verification.md")
    return (last_match(RESULT_RE, text) or "NONE", last_match(FINGERPRINT_RE, text))


# --------------------------------------------------------------------------------------------
# Git helpers
# --------------------------------------------------------------------------------------------

def git(ws: Workspace, *args: str) -> Tuple[int, str]:
    try:
        res = subprocess.run(["git", *args], cwd=ws.root, capture_output=True, text=True,
                             encoding="utf-8", errors="replace")
    except FileNotFoundError:
        return 127, "git not found"
    return res.returncode, (res.stdout + res.stderr).strip()


def is_git_repo(ws: Workspace) -> bool:
    rc, out = git(ws, "rev-parse", "--is-inside-work-tree")
    return rc == 0 and out == "true"


FINGERPRINT_EXCLUDES = [":(exclude).specify", ":(exclude,glob)**/*.md"]


def code_fingerprint(ws: Workspace) -> Optional[str]:
    """Hash of HEAD plus all uncommitted non-doc changes, so stale verification is detectable."""
    if not is_git_repo(ws):
        return None
    h = hashlib.sha256()
    _, head = git(ws, "rev-parse", "HEAD")
    h.update(head.encode())
    _, diff = git(ws, "diff", "HEAD", "--", ".", *FINGERPRINT_EXCLUDES)
    h.update(diff.encode())
    _, untracked = git(ws, "ls-files", "--others", "--exclude-standard", "--", ".", *FINGERPRINT_EXCLUDES)
    for rel in sorted(filter(None, untracked.splitlines())):
        h.update(rel.encode())
        try:
            h.update((ws.root / rel).read_bytes())
        except OSError:
            pass
    return h.hexdigest()[:16]


def resolve_base_ref(ws: Workspace, base: str) -> Optional[str]:
    for ref in (f"origin/{base}", base):
        if git(ws, "rev-parse", "--verify", "--quiet", ref)[0] == 0:
            return ref
    return None


def changed_files_vs_base(ws: Workspace, base: str) -> List[str]:
    files = set()
    ref = resolve_base_ref(ws, base)
    if ref:
        _, out = git(ws, "diff", "--name-only", f"{ref}...HEAD")
        files.update(filter(None, out.splitlines()))
    _, out = git(ws, "diff", "--name-only", "HEAD")
    files.update(filter(None, out.splitlines()))
    _, out = git(ws, "ls-files", "--others", "--exclude-standard")
    files.update(filter(None, out.splitlines()))
    return sorted(files)


def changed_line_count(ws: Workspace, base: str) -> Tuple[int, List[str]]:
    """Added+deleted lines vs base (committed + uncommitted), excluding .specify, docs, and lockfiles."""
    ref = resolve_base_ref(ws, base)
    since = "HEAD"
    if ref:
        rc, mb = git(ws, "merge-base", ref, "HEAD")
        since = mb if rc == 0 else ref
    _, numstat = git(ws, "diff", "--numstat", since, "--", ".", *FINGERPRINT_EXCLUDES)
    total = 0
    counted = []
    for line in numstat.splitlines():
        parts = line.split("\t")
        if len(parts) != 3 or Path(parts[2]).name in LOCKFILE_NAMES:
            continue
        added, deleted = (int(x) if x.isdigit() else 0 for x in parts[:2])
        total += added + deleted
        counted.append(parts[2])
    _, untracked = git(ws, "ls-files", "--others", "--exclude-standard", "--", ".", *FINGERPRINT_EXCLUDES)
    for rel in filter(None, untracked.splitlines()):
        try:
            total += len((ws.root / rel).read_text(encoding="utf-8", errors="ignore").splitlines())
            counted.append(rel)
        except OSError:
            pass
    return total, counted


# --------------------------------------------------------------------------------------------
# Validation
# --------------------------------------------------------------------------------------------

@dataclass
class Issue:
    level: str  # ERROR | WARN
    scope: str
    message: str


def _marker_issues(ws: Workspace, path: Path, scope: str) -> List[Issue]:
    issues = []
    hits = [n for n, line in enumerate(read_text(path).splitlines(), 1)
            if MARKER_RE.search(line) and not line.lstrip().startswith(">")]
    if hits:
        preview = ", ".join(f"L{n}" for n in hits[:8]) + (" ..." if len(hits) > 8 else "")
        issues.append(Issue("ERROR", scope, f"{ws.rel(path)} has {len(hits)} unresolved TODO(sdd) / "
                                            f"[NEEDS CLARIFICATION] markers ({preview})"))
    return issues


def requirement_register(ws: Workspace) -> Optional[set]:
    path = ws.arch / "requirements.md"
    if not path.exists():
        return None
    ids = set()
    for line in path.read_text(encoding="utf-8").splitlines():
        if line.strip().startswith("|"):
            cells = _cells(line)
            if cells and REQ_RE.fullmatch(cells[0]):
                ids.add(cells[0])
    return ids


def validate_module(ws: Workspace, slug: str, stage: str, roadmap: Optional[List[ModuleRow]] = None) -> Tuple[List[Issue], Dict[str, List[str]]]:
    """Returns (issues, id -> test files referencing it)."""
    issues: List[Issue] = []
    coverage: Dict[str, List[str]] = {}
    mod = ws.module_dir(slug)
    spec_path, plan_path, tasks_path = mod / "spec.md", mod / "plan.md", mod / "tasks.md"
    if not spec_path.exists():
        return [Issue("ERROR", slug, f"missing {ws.rel(spec_path)} (run: sdd_engine.py spec --module {slug})")], coverage

    key = module_key(ws, slug, roadmap)
    spec = read_text(spec_path)
    for p in (spec_path, plan_path, tasks_path):
        issues += _marker_issues(ws, p, slug)

    if not re.search(r"^##\s*6\..*Automated Test Criteria", spec, re.MULTILINE | re.IGNORECASE):
        issues.append(Issue("ERROR", slug, "spec.md is missing section '## 6. Automated Test Criteria'"))

    acs = sorted(set(re.findall(rf"\b{re.escape(key)}-AC-\d+\b", spec)))
    e2es = sorted(set(re.findall(rf"\b{re.escape(key)}-E2E-\d+\b", spec)))
    if not acs:
        issues.append(Issue("ERROR", slug, f"spec.md defines no acceptance criteria with IDs '{key}-AC-NN' "
                                           f"(module key '{key}')"))
    if not e2es:
        issues.append(Issue("ERROR", slug, f"spec.md defines no E2E scenarios with IDs '{key}-E2E-NN'"))
    foreign = sorted(set(re.findall(r"\b([A-Z][A-Z0-9]{1,5})-(?:AC|E2E)-\d+\b", spec)) - {key})
    if foreign:
        issues.append(Issue("WARN", slug, f"spec.md references IDs with other module keys {foreign}; "
                                          f"cross-module criteria belong in the owning module"))

    reqs = set(REQ_RE.findall(spec))
    register = requirement_register(ws)
    if register is None:
        issues.append(Issue("WARN", slug, "requirements.md not found; PRD traceability is unchecked"))
    else:
        unknown = sorted(reqs - register)
        if unknown:
            issues.append(Issue("ERROR", slug, f"spec.md references requirements not in requirements.md: {unknown}"))
    if not reqs:
        issues.append(Issue("WARN", slug, "spec.md does not trace to any REQ-NNN requirement"))

    if not tasks_path.exists():
        issues.append(Issue("ERROR", slug, f"missing {ws.rel(tasks_path)}"))
        tasks: List[Task] = []
    else:
        tasks = parse_tasks(read_text(tasks_path), key)
        ids = [t.id for t in tasks if t.id]
        dupes = sorted({i for i in ids if ids.count(i) > 1})
        if dupes:
            issues.append(Issue("ERROR", slug, f"duplicate task IDs: {dupes}"))
        missing_ids = [t.line_no for t in tasks if not t.id]
        if missing_ids:
            issues.append(Issue("WARN", slug, f"tasks.md has {len(missing_ids)} task(s) without '{key}-TNNN' IDs "
                                              f"(lines {missing_ids[:8]})"))
        referenced = {r for t in tasks for r in t.refs}
        unplanned = sorted(set(acs + e2es) - referenced)
        if unplanned:
            issues.append(Issue("ERROR", slug, f"criteria with no implementing task: {unplanned}"))
        undefined = sorted(referenced - set(acs + e2es))
        if undefined:
            issues.append(Issue("ERROR", slug, f"tasks.md references criteria not defined in spec.md: {undefined}"))
        known = set(ids)
        bad_deps = sorted({d for t in tasks for d in t.depends if d not in known})
        if bad_deps:
            issues.append(Issue("ERROR", slug, f"tasks depend on unknown task IDs: {bad_deps}"))
        cycle = find_cycle({t.id: t.depends for t in tasks if t.id})
        if cycle:
            issues.append(Issue("ERROR", slug, f"task dependency cycle: {' -> '.join(cycle)}"))

    if stage == "impl":
        open_tasks = [t.id or f"line {t.line_no}" for t in tasks if not t.done]
        if open_tasks:
            issues.append(Issue("ERROR", slug, f"{len(open_tasks)} task(s) not complete: {open_tasks[:10]}"))
        corpus = ws.test_corpus()
        for cid in acs + e2es:
            # Accept LINK-AC-01 in comments/strings and LINK_AC_01 in identifiers.
            pat = re.compile(r"(?<![A-Za-z0-9])" + "[-_]".join(map(re.escape, cid.split("-"))) + r"(?!\d)")
            coverage[cid] = sorted(f for f, text in corpus.items() if pat.search(text))
        untested = [cid for cid, files in coverage.items() if not files]
        if untested:
            issues.append(Issue("ERROR", slug, f"criteria not referenced by any automated test: {untested} "
                                               f"(put the ID in the test name or a comment)"))
        issues += test_hygiene(ws, slug, key, corpus)
    return issues, coverage


def test_hygiene(ws: Workspace, slug: str, key: str, corpus: Dict[str, str]) -> List[Issue]:
    """Flag focused tests (hide other failures) and skipped tests in files this module touches."""
    issues = []
    # Same ID forms as coverage: KEY-AC-01 in comments/strings, KEY_AC_01 in identifiers.
    id_re = re.compile(rf"(?<![A-Za-z0-9]){re.escape(key)}[-_](?:AC|E2E)[-_]\d+")
    relevant = {f for f, text in corpus.items() if id_re.search(text)}
    if is_git_repo(ws):
        changed = set(changed_files_vs_base(ws, ws.config["base_branch"]))
        relevant |= {f for f in corpus if f in changed}
    focused, skipped = [], []
    for f in sorted(relevant):
        text = corpus[f]
        for n, line in enumerate(text.splitlines(), 1):
            if any(re.search(p, line) for p in FOCUSED_TEST_PATTERNS):
                focused.append(f"{f}:L{n}")
            if any(re.search(p, line) for p in SKIPPED_TEST_PATTERNS):
                skipped.append(f"{f}:L{n}")
    if focused:
        issues.append(Issue("ERROR", slug, f"focused tests (.only / fit) silently exclude other tests: {focused[:10]}"))
    if skipped:
        issues.append(Issue("WARN", slug, f"skipped/ignored tests in module test files (justify in review-log.md): "
                                          f"{skipped[:10]}"))
    return issues


def validate_roadmap(ws: Workspace, roadmap: Optional[List[ModuleRow]]) -> List[Issue]:
    issues: List[Issue] = []
    if roadmap is None:
        return [Issue("WARN", "roadmap", "module-decomposition.md not found")]
    if not roadmap:
        return [Issue("ERROR", "roadmap", "module-decomposition.md has no module table rows")]
    slugs = [r.slug for r in roadmap]
    keys = [r.key for r in roadmap]
    for label, values in (("slug", slugs), ("key", keys)):
        dupes = sorted({v for v in values if v and values.count(v) > 1})
        if dupes:
            issues.append(Issue("ERROR", "roadmap", f"duplicate module {label}s: {dupes}"))
    for r in roadmap:
        if "TODO" in r.slug or "TODO" in r.key or "todo-" in r.slug:
            issues.append(Issue("ERROR", "roadmap", f"template placeholder row still present (slug '{r.slug}')"))
            continue
        if not KEY_RE.match(r.key):
            issues.append(Issue("ERROR", "roadmap", f"module '{r.slug}' has invalid key '{r.key}' "
                                                    f"(2-6 uppercase letters/digits)"))
        unknown = [d for d in r.depends if d not in slugs]
        if unknown:
            issues.append(Issue("ERROR", "roadmap", f"module '{r.slug}' depends on unknown modules {unknown}"))
        if r.status.lower() == "done" and review_status(ws, r.slug) != "APPROVED":
            issues.append(Issue("WARN", "roadmap", f"module '{r.slug}' is Done but review-log is not APPROVED"))
    cycle = find_cycle({r.slug: r.depends for r in roadmap})
    if cycle:
        issues.append(Issue("ERROR", "roadmap", f"module dependency cycle: {' -> '.join(cycle)}"))

    register = requirement_register(ws)
    if register:
        covered = set()
        for r in roadmap:
            covered |= set(r.requirements)
            covered |= set(REQ_RE.findall(read_text(ws.module_dir(r.slug) / "spec.md")))
        orphans = sorted(register - covered)
        if orphans:
            issues.append(Issue("WARN", "roadmap", f"{len(orphans)} requirement(s) not owned by any module or spec: "
                                                   f"{orphans[:15]}"))
    return issues


def report_issues(issues: List[Issue]) -> int:
    errors = [i for i in issues if i.level == "ERROR"]
    warns = [i for i in issues if i.level == "WARN"]
    for i in errors + warns:
        say(f"  {i.level:<5} [{i.scope}] {i.message}")
    say(f"\n  {len(errors)} error(s), {len(warns)} warning(s)")
    return 1 if errors else 0


# --------------------------------------------------------------------------------------------
# Command detection & verification
# --------------------------------------------------------------------------------------------

def detect_commands(root: Path) -> Dict[str, Optional[str]]:
    has = lambda *names: any((root / n).exists() for n in names)  # noqa: E731
    globbed = lambda *pats: any(any(root.glob(p)) for p in pats)  # noqa: E731
    if globbed("*.sln", "*.slnx", "*.csproj"):
        return {"lint": None, "build": "dotnet build --warnaserror", "test": "dotnet test --no-build"}
    if has("package.json"):
        try:
            scripts = json.loads((root / "package.json").read_text(encoding="utf-8")).get("scripts", {})
        except (json.JSONDecodeError, OSError):
            scripts = {}
        return {"lint": "npm run lint" if "lint" in scripts else None,
                "build": "npm run build" if "build" in scripts else None,
                "test": "npm test" if "test" in scripts else None}
    if has("go.mod"):
        return {"lint": "go vet ./...", "build": "go build ./...", "test": "go test ./..."}
    if has("Cargo.toml"):
        return {"lint": "cargo clippy --all-targets -- -D warnings", "build": None, "test": "cargo test"}
    if has("pyproject.toml", "setup.cfg", "setup.py", "pytest.ini", "tox.ini", "requirements.txt"):
        return {"lint": "ruff check ." if shutil.which("ruff") else None, "build": None, "test": "pytest"}
    return {"lint": None, "build": None, "test": None}


def resolve_commands(ws: Workspace, slug: Optional[str]) -> Dict[str, Optional[str]]:
    cmds = detect_commands(ws.root)
    for k, v in ws.config.get("commands", {}).items():
        if v:
            cmds[k] = v
    if slug:
        for k, v in ws.config.get("module_commands", {}).get(slug, {}).items():
            if v:
                cmds[k] = v
    return cmds


def parse_test_counts(output: str) -> Dict[str, Optional[int]]:
    """Best-effort counts from common runners. The exit code, not these numbers, decides pass/fail."""
    counts: Dict[str, Optional[int]] = {"passed": None, "failed": None, "skipped": None}
    patterns = {
        # dotnet summary (one line for the whole run): "total: 12, failed: 0, succeeded: 12, skipped: 0"
        "dotnet-summary": {"passed": r"total:\s*\d+, failed:\s*\d+, succeeded:\s*(\d+)",
                           "failed": r"total:\s*\d+, failed:\s*(\d+)",
                           "skipped": r"succeeded:\s*\d+, skipped:\s*(\d+)"},
        # dotnet classic (one line per test project, summed): "Passed!  - Failed:     0, Passed:    12, Skipped:     0"
        "dotnet": {"passed": r"Passed:\s+(\d+)", "failed": r"Failed:\s+(\d+)", "skipped": r"Skipped:\s+(\d+)"},
        # jest: "Tests:       1 failed, 2 skipped, 10 passed, 13 total"
        "jest": {"passed": r"Tests:.*?(\d+) passed", "failed": r"Tests:.*?(\d+) failed", "skipped": r"Tests:.*?(\d+) skipped"},
        # vitest: "Tests  10 passed | 1 failed (11)"
        "vitest": {"passed": r"Tests\s+.*?(\d+) passed", "failed": r"Tests\s+.*?(\d+) failed", "skipped": r"Tests\s+.*?(\d+) skipped"},
        # pytest: "=== 10 passed, 1 failed, 2 skipped in 0.12s ===" (-q drops the "=" rule)
        "pytest": {k: rf"(?m)^=*\s*(?:\d+ [a-z]+, )*(\d+) {word}\b.* in [\d.]+s"
                   for k, word in (("passed", "passed"), ("failed", "failed"), ("skipped", "skipped"))},
        # cargo: "test result: ok. 10 passed; 0 failed; 1 ignored" (one line per test binary, summed)
        "cargo": {"passed": r"test result: .*?(\d+) passed", "failed": r"test result: .*?(\d+) failed",
                  "skipped": r"test result: .*?(\d+) ignored"},
    }
    for runner, pats in patterns.items():
        found = {}
        for k, p in pats.items():
            matches = re.findall(p, output)
            nums = [int(next(g for g in m if g)) if isinstance(m, tuple) else int(m) for m in matches]
            if nums:
                found[k] = sum(nums) if runner in ("cargo", "dotnet") else nums[-1]
        if "passed" in found or "failed" in found:
            counts.update(found)
            return counts
    ran = re.findall(r"^Ran (\d+) tests? in", output, re.M)  # python unittest
    if ran:
        total = int(ran[-1])
        # "expected failures=N" are passing outcomes, not failures.
        num = lambda name: sum(int(n) for n in re.findall(rf"(?<!expected )\b{name}=(\d+)", output))  # noqa: E731
        failed, skipped = num("failures") + num("errors"), num("skipped")
        counts.update({"passed": total - failed - skipped, "failed": failed, "skipped": skipped})
        return counts
    go_pass, go_fail = len(re.findall(r"^--- PASS", output, re.M)), len(re.findall(r"^--- FAIL", output, re.M))
    if go_pass or go_fail:
        counts.update({"passed": go_pass, "failed": go_fail,
                       "skipped": len(re.findall(r"^--- SKIP", output, re.M))})
    return counts


@dataclass
class StepResult:
    step: str
    command: Optional[str]
    exit_code: Optional[int]
    seconds: float
    log: Optional[str]
    note: str = ""


def run_step(ws: Workspace, slug: str, step: str, command: Optional[str], stamp: str) -> Tuple[StepResult, str]:
    if not command:
        return StepResult(step, None, None, 0.0, None, "not configured"), ""
    say(f"  running  {step}: {command}")
    log_dir = ws.runs / slug
    log_dir.mkdir(parents=True, exist_ok=True)
    log_path = log_dir / f"{stamp}-{step}.log"
    start = time.monotonic()
    try:
        res = subprocess.run(command, shell=True, cwd=ws.root, capture_output=True, text=True,
                             encoding="utf-8", errors="replace",
                             timeout=ws.config.get("command_timeout_seconds", 1800))
        output, code, note = res.stdout + "\n" + res.stderr, res.returncode, ""
    except subprocess.TimeoutExpired as exc:
        text = lambda b: b.decode("utf-8", "replace") if isinstance(b, bytes) else (b or "")  # noqa: E731
        output = f"{text(exc.stdout)}\n{text(exc.stderr)}\nTIMEOUT after {exc.timeout}s"
        code, note = 124, "timed out"
    elapsed = time.monotonic() - start
    log_path.write_text(f"$ {command}\n\n{output}", encoding="utf-8")
    say(f"           exit {code} in {elapsed:.1f}s -> {ws.rel(log_path)}")
    return StepResult(step, command, code, elapsed, ws.rel(log_path), note), output


# --------------------------------------------------------------------------------------------
# Commands
# --------------------------------------------------------------------------------------------

@dataclass
class InterviewQuestion:
    id: str
    category: str
    question: str
    options: List[str]
    default: str


INTERVIEW = [
    InterviewQuestion("arch_style", "System Architecture", "Which architectural style should the system follow?",
                      ["Modular Monolith (single deployable, isolated domain modules)",
                       "Microservices (independent deployables, event-driven)",
                       "Clean Architecture / Hexagonal single service",
                       "Follow the existing codebase architecture (brownfield)"],
                      "Modular Monolith (single deployable, isolated domain modules)"),
    InterviewQuestion("primary_db", "Persistence", "What is the primary database engine?",
                      ["PostgreSQL", "SQL Server / MySQL", "MongoDB", "SQLite (local/dev only)", "Existing database (brownfield)"],
                      "PostgreSQL"),
    InterviewQuestion("messaging", "Async Communication", "How should asynchronous events be processed?",
                      ["RabbitMQ", "Kafka / Event Hubs", "Redis Streams", "In-process domain events only", "None needed"],
                      "In-process domain events only"),
    InterviewQuestion("auth", "Security", "Which authentication mechanism?",
                      ["OAuth2 / OpenID Connect (external IdP)", "JWT Bearer issued by this system",
                       "Session cookies + CSRF", "API key + HMAC", "Existing mechanism (brownfield)"],
                      "OAuth2 / OpenID Connect (external IdP)"),
    InterviewQuestion("e2e", "Testing", "How are automated E2E scenarios executed?",
                      ["In-process host (WebApplicationFactory / Supertest / TestClient) + Testcontainers",
                       "Playwright against a deployed environment", "Cypress", "Pytest + HTTPX"],
                      "In-process host (WebApplicationFactory / Supertest / TestClient) + Testcontainers"),
]


def conduct_interview(ws: Workspace, interactive: bool, reinterview: bool) -> Dict[str, str]:
    answers_file = ws.arch / "interview-answers.json"
    if answers_file.exists() and not reinterview:
        say(f"  using saved answers from {ws.rel(answers_file)} (use --reinterview to change)")
        return json.loads(answers_file.read_text(encoding="utf-8"))
    answers: Dict[str, str] = {}
    interactive = interactive and sys.stdin.isatty()
    for q in INTERVIEW:
        say(f"\n[{q.category}] {q.question}")
        for idx, opt in enumerate(q.options, 1):
            say(f"  {'*' if opt == q.default else ' '} {idx}. {opt}")
        choice = ""
        if interactive:
            try:
                choice = input(f"Choose 1-{len(q.options)} (Enter = default): ").strip()
            except (EOFError, KeyboardInterrupt):
                choice = ""
        answers[q.id] = q.options[int(choice) - 1] if choice.isdigit() and 1 <= int(choice) <= len(q.options) else q.default
        if not interactive:
            say(f"  default: {q.default}")
    answers_file.parent.mkdir(parents=True, exist_ok=True)
    answers_file.write_text(json.dumps(answers, indent=2), encoding="utf-8")
    return answers


def extract_requirement_candidates(prd: str) -> List[Tuple[str, str]]:
    """Pull bullet/numbered lines that read like requirements, with their nearest heading."""
    out, section = [], "Intro"
    modal = re.compile(r"\b(must|shall|should|will|can|needs? to|required?|allows?|supports?)\b", re.I)
    for line in prd.splitlines():
        s = line.strip()
        if s.startswith("#"):
            section = s.lstrip("#").strip() or section
            continue
        m = re.match(r"^(?:[-*+]|\d+[.)])\s+(.*)$", s)
        if m and len(m.group(1)) > 12 and modal.search(m.group(1)):
            out.append((m.group(1).replace("|", "/").strip(), section.replace("|", "/")))
    return out


def cmd_intake(ws: Workspace, args) -> int:
    heading("Intake: scaffolding architecture artifacts")
    prd_text, prd_ref = args.prd, "inline text"
    for candidate in (Path(args.prd), ws.root / args.prd):
        try:
            if candidate.is_file():
                prd_text, prd_ref = candidate.read_text(encoding="utf-8"), ws.rel(candidate.resolve())
                break
        except OSError:
            continue
    if prd_ref == "inline text":
        if len(args.prd) < 40:
            say(f"error: PRD file not found: {args.prd}")
            return 1
        (ws.arch).mkdir(parents=True, exist_ok=True)
        (ws.arch / "prd.md").write_text(prd_text, encoding="utf-8")
        prd_ref = ws.rel(ws.arch / "prd.md")
    say(f"  PRD: {prd_ref} ({len(prd_text.splitlines())} lines)")

    answers = conduct_interview(ws, interactive=not args.non_interactive, reinterview=args.reinterview)

    # Requirements register with heuristic candidates; agent must confirm each (TODO(sdd) stays until then).
    req = ws.render_template("requirements-template.md")
    req = req.replace("> Source PRD: `TODO(sdd): path`", f"> Source PRD: `{prd_ref}`")
    req = req.replace("Extracted: TODO(sdd): date", f"Extracted: {datetime.now():%Y-%m-%d} (candidates, confirm each)")
    candidates = extract_requirement_candidates(prd_text)
    if candidates:
        rows = [f"| REQ-{i:03d} | {text} | TODO(sdd) | TODO(sdd) | {sec} | TODO(sdd) | candidate |"
                for i, (text, sec) in enumerate(candidates, 1)]
        req = re.sub(r"^\| REQ-001 \|.*$", "\n".join(rows), req, count=1, flags=re.MULTILINE)
    ws.write_artifact(ws.arch / "requirements.md", req, args.force)
    say(f"           {len(candidates)} candidate requirement(s) extracted heuristically")

    decisions = "\n".join(f"- **{q.category}**: {answers.get(q.id, q.default)}" for q in INTERVIEW)
    arch = f"""# System Architecture

> Source PRD: `{prd_ref}` | Requirements: `requirements.md` | Context: `../memory/project-context.md`

## 1. Key Decisions (interview)
{decisions}

## 2. System Topology
TODO(sdd): Mermaid diagram of the real components, data stores, and integrations named in the PRD.

## 3. Domain Model & Bounded Contexts
TODO(sdd): entities, invariants, and which module owns each.

## 4. API, Event & Versioning Standards
TODO(sdd)

## 5. Security & Cross-Cutting Concerns
TODO(sdd): authn/authz flow, tenancy isolation, logging, tracing, resilience.

## 6. Test Strategy & Quality Gates
TODO(sdd): unit / integration / E2E split, environments, test data strategy.

## 7. Architecture Decision Records
| ADR | Decision | Alternatives Considered | Consequences |
| :--- | :--- | :--- | :--- |
"""
    ws.write_artifact(ws.arch / "system-architecture.md", arch, args.force)
    ws.write_artifact(ws.arch / "module-decomposition.md",
                      ws.render_template("module-decomposition-template.md"), args.force)

    ctx = ws.render_template("project-context-template.md")
    detected = detect_commands(ws.root)
    ctx += "\n## Detected by sdd_engine.py (unverified)\n" + "\n".join(
        f"- **{k}**: `{v}`" if v else f"- **{k}**: not detected" for k, v in detected.items()) + "\n"
    ws.write_artifact(ws.specify / "memory" / "project-context.md", ctx, args.force)

    say("\nNext: the /sdd-intake agent skill fills every TODO(sdd), then run "
        "`python scripts/sdd_engine.py validate --all`.")
    return 0


def cmd_spec(ws: Workspace, args) -> int:
    slug = args.module
    if not re.fullmatch(r"[a-z0-9]+(?:-[a-z0-9]+)*", slug):
        say(f"error: module slug must be kebab-case: {slug}")
        return 1
    roadmap = parse_roadmap(ws) or []
    row = next((r for r in roadmap if r.slug == slug), None)
    key = args.key or (row.key if row and KEY_RE.match(row.key or "") else derive_key(slug))
    if not KEY_RE.match(key):
        say(f"error: invalid module key '{key}' (2-6 uppercase letters/digits)")
        return 1
    clash = [r.slug for r in roadmap if r.key == key and r.slug != slug]
    if clash:
        say(f"error: key '{key}' is already used by module(s) {clash}; pass --key")
        return 1
    if roadmap and not row:
        say(f"  warning: '{slug}' is not in module-decomposition.md; add a row so dependencies are tracked")
    title = args.title or (row.title if row and row.title else slug.replace("-", " ").title())

    heading(f"Spec: scaffolding {slug} (key {key})")
    mod = ws.module_dir(slug)
    for template, name in (("spec-template.md", "spec.md"), ("plan-template.md", "plan.md"),
                           ("tasks-template.md", "tasks.md"), ("checklist-template.md", "checklist.md")):
        ws.write_artifact(mod / name, ws.render_template(template, slug, title, key), args.force)
    say(f"\nNext: /sdd-spec {slug} fills the TODO(sdd) markers, then "
        f"`python scripts/sdd_engine.py validate --module {slug}`.")
    return 0


def cmd_validate(ws: Workspace, args) -> int:
    roadmap = parse_roadmap(ws)
    issues: List[Issue] = []
    if args.all:
        heading(f"Validate: roadmap + all modules (stage: {args.stage})")
        for name in ("requirements.md", "system-architecture.md", "module-decomposition.md"):
            issues += _marker_issues(ws, ws.arch / name, "architecture")
        issues += _marker_issues(ws, ws.specify / "memory" / "project-context.md", "architecture")
        issues += validate_roadmap(ws, roadmap)
        slugs = sorted({p.name for p in ws.specs.iterdir() if p.is_dir()} if ws.specs.exists() else set())
        for slug in slugs:
            issues += validate_module(ws, slug, args.stage, roadmap)[0]
    else:
        if not args.module:
            say("error: pass --module <slug> or --all")
            return 1
        heading(f"Validate: {args.module} (stage: {args.stage})")
        issues += validate_module(ws, args.module, args.stage, roadmap)[0]
    return report_issues(issues)


def cmd_verify(ws: Workspace, args) -> int:
    slug = args.module
    mod = ws.module_dir(slug)
    label = "baseline" if args.baseline else "verification"
    heading(f"Verify ({label}): {slug}")
    stamp = datetime.now().strftime("%Y%m%d-%H%M%S")
    fingerprint = code_fingerprint(ws)
    cmds = resolve_commands(ws, slug)

    results: List[StepResult] = []
    test_output = ""
    for step in ("lint", "build", "test"):
        res, output = run_step(ws, slug, step, cmds.get(step), stamp)
        results.append(res)
        if step == "test":
            test_output = output
    counts = parse_test_counts(test_output) if test_output else {"passed": None, "failed": None, "skipped": None}

    failures = [r for r in results if r.exit_code not in (None, 0)]
    no_tests = cmds.get("test") is None
    issues: List[Issue] = []
    if not args.baseline and (mod / "spec.md").exists():
        issues, _ = validate_module(ws, slug, "impl")
    trace_errors = [i for i in issues if i.level == "ERROR"]
    passed = not failures and not no_tests and not trace_errors

    rows = "\n".join(
        f"| {r.step} | `{r.command}` | {r.exit_code} | {r.seconds:.1f}s | `{r.log}` |" if r.command
        else f"| {r.step} | not configured | - | - | - |" for r in results)
    fmt = lambda v: "unknown" if v is None else str(v)  # noqa: E731
    issue_lines = "\n".join(f"- **{i.level}** {i.message}" for i in issues) or "- None"
    reasons = [f"{r.step} exited {r.exit_code}" for r in failures]
    if no_tests:
        reasons.append("no test command configured or detected (set commands.test in .specify/config.json)")
    if trace_errors:
        reasons.append(f"{len(trace_errors)} traceability/hygiene error(s)")
    report = f"""# {"Baseline" if args.baseline else "Verification"} Report: `{slug}`

- **Generated**: {datetime.now():%Y-%m-%d %H:%M:%S}
- **Code fingerprint**: `{fingerprint or "n/a (not a git repo)"}` (HEAD + uncommitted non-doc changes)
- **Failure reasons**: {"; ".join(reasons) or "None"}

## Commands
| Step | Command | Exit | Duration | Log |
| :--- | :--- | :--- | :--- | :--- |
{rows}

## Test Counts (parsed from runner output; exit code is authoritative)
- Passed: {fmt(counts["passed"])}
- Failed: {fmt(counts["failed"])}
- Skipped: {fmt(counts["skipped"])}

## Traceability & Test Hygiene
{issue_lines}

RESULT: {"PASS" if passed else "FAIL"}
FINGERPRINT: {fingerprint or "none"}
"""
    out = mod / ("verification-baseline.md" if args.baseline else "verification.md")
    mod.mkdir(parents=True, exist_ok=True)
    out.write_text(report, encoding="utf-8")
    say(f"\n  RESULT: {'PASS' if passed else 'FAIL'} -> {ws.rel(out)}")
    for r in reasons:
        say(f"    - {r}")
    if args.baseline:
        return 0
    return 0 if passed else 1


def cmd_status(ws: Workspace, args) -> int:
    roadmap = parse_roadmap(ws)
    heading("Module roadmap")
    if not roadmap:
        say("  no module-decomposition.md rows yet (run intake)")
        return 0
    done = {r.slug for r in roadmap if r.status.lower() == "done"}
    rows = []
    for r in roadmap:
        mod = ws.module_dir(r.slug)
        blocked_by = [d for d in r.depends if d not in done]
        spec_errors = "-"
        if (mod / "spec.md").exists():
            n = sum(1 for i in validate_module(ws, r.slug, "spec", roadmap)[0] if i.level == "ERROR")
            spec_errors = "ok" if n == 0 else f"{n} err"
        rows.append([r.phase, r.slug, r.key, ",".join(r.depends) or "-", r.status or "-",
                     "yes" if not blocked_by else "no", spec_errors, review_status(ws, r.slug),
                     verification_result(ws, r.slug)[0]])
    print_table(["Ph", "Slug", "Key", "Depends", "Status", "Unblocked", "Spec", "Review", "Verify"], rows)
    ready = [r.slug for r in roadmap if r.status.lower() in ("planned", "specified")
             and all(d in done for d in r.depends)]
    if ready:
        say(f"\n  Can start now (in parallel): {', '.join(ready)}")
    return 0


def cmd_loop(ws: Workspace, args) -> int:
    if not args.mock:
        say("The generator/reviewer loop is performed by the agent skill `/sdd-loop <module>`, not by this CLI.\n"
            "This CLI checks the result: `python scripts/sdd_engine.py verify --module <module>`.\n"
            "Use --mock to print a simulated loop for demos.")
        return 2
    slug = args.module
    heading(f"SIMULATED loop for {slug} (no code is generated or reviewed)")
    log = ws.module_dir(slug) / "review-log.md"
    log.parent.mkdir(parents=True, exist_ok=True)
    entries = [
        ("REJECTED", ["[simulated] Major: AC-02 returns 500 instead of RFC 7807 400.",
                      "[simulated] Major: E2E-02 lacks a 401 assertion."]),
        ("SIMULATED", ["[simulated] All findings resolved. Not a real approval; `pr` will refuse this status."]),
    ]
    body = f"# Review Log: `{slug}` (SIMULATED)\n\n"
    for n, (status, findings) in enumerate(entries[: max(1, args.iterations)], 1):
        say(f"  iteration {n}: {status}")
        body += f"## Iteration {n}\n" + "\n".join(f"- {f}" for f in findings) + f"\n\nSTATUS: {status}\n\n"
    log.write_text(body, encoding="utf-8")
    say(f"  wrote {ws.rel(log)}")
    return 0


SECRET_RES = [re.compile(p, re.I) for p in SECRET_FILE_PATTERNS]


def build_pr_body(ws: Workspace, slug: str, coverage: Dict[str, List[str]]) -> str:
    mod = ws.module_dir(slug)
    spec = read_text(mod / "spec.md")
    purpose = re.search(r"\*\*Module Purpose\*\*:\s*(.+)", spec)
    title_m = re.search(r"^# Specification:\s*(.+?)\s*\(", spec, re.M)
    reqs = sorted(set(REQ_RE.findall(spec)))
    review = read_text(mod / "review-log.md")
    iterations = len(re.findall(r"^## Iteration", review, re.M))
    verification = read_text(mod / "verification.md")
    counts = re.search(r"## Test Counts.*?\n(- Passed.*?\n- Failed.*?\n- Skipped.*?)\n", verification, re.S)
    cov_rows = "\n".join(f"| {cid} | {', '.join(f'`{f}`' for f in files[:3]) or 'NONE'} |"
                         for cid, files in sorted(coverage.items())) or "| - | - |"
    return f"""## Summary
{purpose.group(1).strip() if purpose else "See spec."}

**Module**: {title_m.group(1) if title_m else slug} (`{slug}`)
**Requirements**: {", ".join(reqs) or "none listed"}

## Spec Artifacts
- Spec: `.specify/specs/{slug}/spec.md`
- Plan: `.specify/specs/{slug}/plan.md`
- Tasks: `.specify/specs/{slug}/tasks.md`
- Review log ({iterations} iteration(s), final status `{review_status(ws, slug)}`): `.specify/specs/{slug}/review-log.md`
- Verification: `.specify/specs/{slug}/verification.md`

## Acceptance Criteria -> Tests
| Criterion | Referenced by |
| :--- | :--- |
{cov_rows}

## Verification (measured by `sdd_engine.py verify`)
Result: `{verification_result(ws, slug)[0]}`
{counts.group(1) if counts else "- Test counts unavailable"}

## Human Reviewer Checklist
- [ ] Behaviour matches the acceptance criteria (not just the tests)
- [ ] No breaking change to shared contracts or migrations without a rollout plan
- [ ] Tests assert outcomes, not implementation details; nothing was skipped to go green
"""


def cmd_pr(ws: Workspace, args) -> int:
    slug = args.module
    base = args.base or ws.config["base_branch"]
    heading(f"PR pre-flight: {slug} -> {base}")
    gates: List[str] = []

    rs = review_status(ws, slug)
    if rs != "APPROVED":
        gates.append(f"review-log.md final status is {rs}, expected APPROVED")
    vr, vfp = verification_result(ws, slug)
    if vr != "PASS":
        gates.append(f"verification.md result is {vr}, expected PASS (run verify)")
    fp = code_fingerprint(ws)
    if vr == "PASS" and fp and vfp and fp != vfp:
        gates.append("code changed after the last verify (fingerprint mismatch); rerun verify")
    issues, coverage = validate_module(ws, slug, "impl")
    errs = [i for i in issues if i.level == "ERROR"]
    if errs:
        gates.append(f"{len(errs)} validation error(s): " + "; ".join(i.message for i in errs[:3]))

    git_ok = is_git_repo(ws)
    if not git_ok:
        gates.append("not a git repository")
    else:
        lines, _ = changed_line_count(ws, base)
        limit = int(ws.config.get("max_pr_changed_lines", 600))
        if lines > limit and not args.allow_large:
            gates.append(f"{lines} changed lines exceeds max_pr_changed_lines={limit}; split into stacked PRs "
                         f"(see plan.md delivery slices) or pass --allow-large")
        else:
            say(f"  size     {lines} changed lines (limit {limit})")
        _, status_out = git(ws, "status", "--porcelain", "-uall")
        pending = [line[3:].strip().strip('"') for line in status_out.splitlines() if line.strip()]
        secrets = [p for p in pending if any(r.search(p) for r in SECRET_RES)]
        if secrets:
            gates.append(f"refusing to commit likely secret files: {secrets}")

    for g in gates:
        say(f"  GATE FAIL  {g}")
    if gates and not args.force:
        say("\nPR not created. Fix the gates above (or pass --force to override, which is recorded in the PR body).")
        return 1
    if not gates:
        say("  all gates passed")

    body = build_pr_body(ws, slug, coverage)
    if gates:
        body += "\n## Gates overridden with --force\n" + "\n".join(f"- {g}" for g in gates) + "\n"
    body_file = ws.module_dir(slug) / "pr-body.md"
    body_file.write_text(body, encoding="utf-8")
    say(f"  wrote    {ws.rel(body_file)}")

    _, branch = git(ws, "rev-parse", "--abbrev-ref", "HEAD")
    target = branch if branch not in (base, "HEAD", "main", "master") else f"{ws.config['branch_prefix']}{slug}"
    title = f"feat({slug}): {args.title or 'implement ' + slug.replace('-', ' ')}"
    if args.dry_run:
        say(f"\nDry run: would commit on '{target}', push, and open PR '{title}' against '{base}'.")
        return 0

    def must(*cmd: str) -> bool:
        rc, out = git(ws, *cmd)
        if rc != 0:
            say(f"error: git {' '.join(cmd)} failed:\n{out}")
        return rc == 0

    if target != branch:
        exists = git(ws, "rev-parse", "--verify", "--quiet", target)[0] == 0
        if not must(*(["switch", target] if exists else ["switch", "-c", target])):
            return 1
    if not must("add", "-A"):
        return 1
    if git(ws, "diff", "--cached", "--quiet")[0] != 0:
        if not must("commit", "-m", title):
            return 1
    if not must("push", "-u", "origin", target):
        return 1

    if not shutil.which("gh"):
        say(f"gh CLI not found. Branch pushed; open the PR manually with body {ws.rel(body_file)}.")
        return 1
    view = subprocess.run(["gh", "pr", "view", target, "--json", "url", "-q", ".url"], cwd=ws.root,
                          capture_output=True, text=True)
    if view.returncode == 0 and view.stdout.strip():
        say(f"PR already exists, updated by push: {view.stdout.strip()}")
        return 0
    cmd = ["gh", "pr", "create", "--base", base, "--head", target, "--title", title, "--body-file", str(body_file)]
    if args.draft:
        cmd.append("--draft")
    res = subprocess.run(cmd, cwd=ws.root, capture_output=True, text=True)
    if res.returncode != 0:
        say(f"error: gh pr create failed:\n{res.stderr.strip()}")
        return 1
    say(f"PR opened: {res.stdout.strip()}\nMerging is a human decision after CI and review pass.")
    return 0


def cmd_run_all(ws: Workspace, args) -> int:
    say("SMOKE TEST: scaffolds artifacts and runs every gate. Expect validation failures until an agent "
        "fills the TODO(sdd) markers and implements the module.")
    args.non_interactive, args.reinterview = True, False
    if cmd_intake(ws, args):
        return 1
    args.key, args.title = None, None
    cmd_spec(ws, args)
    args.all, args.stage = False, "spec"
    spec_rc = cmd_validate(ws, args)
    if args.mock:
        args.iterations = 2
        cmd_loop(ws, args)
    args.base, args.dry_run, args.allow_large, args.draft = None, True, False, False
    pr_rc = cmd_pr(ws, args)
    heading("Smoke test summary")
    say(f"  spec validation: {'pass' if spec_rc == 0 else 'fail (expected for fresh scaffolds)'}")
    say(f"  pr gates:        {'pass' if pr_rc == 0 else 'fail (expected until verified and approved)'}")
    return 0


# --------------------------------------------------------------------------------------------
# CLI
# --------------------------------------------------------------------------------------------

def build_parser() -> argparse.ArgumentParser:
    p = argparse.ArgumentParser(description="SDD Engine: scaffolding and verification for spec-driven delivery")
    p.add_argument("--root", type=Path, default=None, help="Repository root (default: toolkit's parent dir)")
    sub = p.add_subparsers(dest="command")

    s = sub.add_parser("intake", help="Scaffold requirements, architecture, roadmap, project context")
    s.add_argument("--prd", default="docs/prd.md", help="PRD file path or raw PRD text")
    s.add_argument("--non-interactive", action="store_true", help="Accept default interview answers")
    s.add_argument("--reinterview", action="store_true", help="Ignore saved interview answers")
    s.add_argument("--force", action="store_true", help="Overwrite existing artifacts")

    s = sub.add_parser("spec", help="Scaffold a module spec suite from templates")
    s.add_argument("--module", required=True)
    s.add_argument("--key", help="Module key used in IDs (default: roadmap Key column or derived)")
    s.add_argument("--title")
    s.add_argument("--force", action="store_true", help="Overwrite existing spec files")

    s = sub.add_parser("validate", help="Check traceability, markers, and roadmap consistency")
    g = s.add_mutually_exclusive_group()
    g.add_argument("--module")
    g.add_argument("--all", action="store_true")
    s.add_argument("--stage", choices=["spec", "impl"], default="spec",
                   help="spec: before coding; impl: also require tests and completed tasks")

    s = sub.add_parser("verify", help="Run lint/build/test and record real results")
    s.add_argument("--module", required=True)
    s.add_argument("--baseline", action="store_true", help="Record pre-change state (never fails)")

    sub.add_parser("status", help="Show roadmap readiness and gate state")

    s = sub.add_parser("loop", help="Simulated loop for demos (--mock)")
    s.add_argument("--module", required=True)
    s.add_argument("--iterations", type=int, default=2)
    s.add_argument("--mock", action="store_true")

    s = sub.add_parser("pr", help="Enforce gates, commit, push, and open a PR (never merges)")
    s.add_argument("--module", required=True)
    s.add_argument("--base", help="Base branch (default from config; use a feature branch for stacked PRs)")
    s.add_argument("--title")
    s.add_argument("--draft", action="store_true")
    s.add_argument("--dry-run", action="store_true")
    s.add_argument("--allow-large", action="store_true", help="Allow a PR above max_pr_changed_lines")
    s.add_argument("--force", action="store_true", help="Override failed gates (recorded in PR body)")

    s = sub.add_parser("run-all", help="Smoke test the toolchain end to end")
    s.add_argument("--prd", default="docs/prd.md")
    s.add_argument("--module", default="auth-service")
    s.add_argument("--mock", action="store_true")
    s.add_argument("--force", action="store_true")
    return p


def main(argv: Optional[Iterable[str]] = None) -> int:
    parser = build_parser()
    args = parser.parse_args(argv)
    if not args.command:
        parser.print_help()
        return 0
    ws = Workspace((args.root or DEFAULT_ROOT).resolve())
    handlers = {"intake": cmd_intake, "spec": cmd_spec, "validate": cmd_validate, "verify": cmd_verify,
                "status": cmd_status, "loop": cmd_loop, "pr": cmd_pr, "run-all": cmd_run_all}
    return handlers[args.command](ws, args)


if __name__ == "__main__":
    sys.exit(main())
