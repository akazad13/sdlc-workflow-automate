"""Shared helpers for the toolkit test suite (stdlib only).

Fixture IDs are always composed at runtime (f"{K}-AC-01") and focused/skip markers are split
across string literals, so these sources never count as coverage or trip hygiene checks when
the toolkit is copied into a real project.
"""

from __future__ import annotations

import contextlib
import importlib.util
import io
import json
import os
import shutil
import stat
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path
from typing import Dict, Optional

REPO_ROOT = Path(__file__).resolve().parents[2]
ENGINE_PATH = REPO_ROOT / "scripts" / "sdd_engine.py"
AGENT_PATH = REPO_ROOT / ".github" / "scripts" / "pr_review_agent.py"
TEMPLATES = REPO_ROOT / ".specify" / "templates"

K = "ZQX"
SLUG = "link-shortener"
ONLY = "it" + ".only("
SKIP = "it" + ".skip("


def load_module(name: str, path: Path):
    if name in sys.modules:
        return sys.modules[name]
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    sys.modules[name] = module
    spec.loader.exec_module(module)
    return module


engine = load_module("sdd_engine", ENGINE_PATH)
agent = load_module("pr_review_agent", AGENT_PATH)


def _force_remove(func, path, _exc):
    os.chmod(path, stat.S_IWRITE)
    func(path)


def rmtree(path: Path) -> None:
    if sys.version_info >= (3, 12):
        shutil.rmtree(path, onexc=_force_remove)
    else:
        shutil.rmtree(path, onerror=_force_remove)


def capture(fn, *args, **kwargs):
    """Run fn with stdout captured; return (result, output)."""
    buf = io.StringIO()
    with contextlib.redirect_stdout(buf):
        result = fn(*args, **kwargs)
    return result, buf.getvalue()


def write(root: Path, rel: str, text: str) -> Path:
    path = root / rel
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(text, encoding="utf-8")
    return path


def python_cmd(code: str) -> str:
    """Shell command running `code` with this interpreter (works in sh and cmd.exe)."""
    return f'"{sys.executable}" -c "{code}"'


# --------------------------------------------------------------------------------------------
# Module fixture: a fully valid, implemented, approved module
# --------------------------------------------------------------------------------------------

def spec_md() -> str:
    return f"""# Specification: Link Shortener (`{SLUG}`)

**Module Key:** `{K}` | **Status:** Draft | **Depends on:** None

> ID rules: acceptance criteria use `{K}-AC-NN`. A TODO(sdd) in a note line is ignored.

## 1. Executive Summary & Scope
- **Module Purpose**: Turns long URLs into short codes.
- **PRD Requirements Covered**: `REQ-001`

## 3. Acceptance Criteria (Given-When-Then)
- **{K}-AC-01 (Happy path)** Traces: REQ-001
- **{K}-AC-02 (Input validation failure)** Traces: REQ-001

## 6. Automated Test Criteria (MANDATORY GATE)
- [ ] **{K}-E2E-01: Full lifecycle** (covers {K}-AC-01, {K}-AC-02)
"""


def tasks_md(done: bool = True) -> str:
    x = "x" if done else " "
    return f"""# Implementation Tasks

> Format: `- [ ] {K}-T001 Description (AC: ids) (Depends: ids | none)`

- [{x}] {K}-T001 Domain model (AC: {K}-AC-01) (Files: src/link.py) (Depends: none)
- [{x}] {K}-T002 Validation (AC: {K}-AC-02) (Depends: {K}-T001)
- [{x}] {K}-T003 End to end (AC: {K}-E2E-01) (Depends: {K}-T001, {K}-T002)
"""


def test_py() -> str:
    # AC-01 and E2E-01 in the hyphen form (comments), AC-02 in the identifier form.
    return f"""# {K}-AC-01: shortening returns a code
def test_shorten():
    assert True


def test_{K}_AC_02_rejects_bad_url():
    assert True


def test_lifecycle():  # {K}-E2E-01
    assert True
"""


REQUIREMENTS_MD = """# Requirements Register

| ID | Requirement | Section |
| :--- | :--- | :--- |
| REQ-001 | Users can shorten a link | Core |
"""


def roadmap_md(rows: Optional[str] = None) -> str:
    rows = rows if rows is not None else f"| 1 | Link Shortener | `{SLUG}` | {K} | None | REQ-001 | In Progress |"
    return f"""# Module Decomposition & Execution Roadmap

| Phase | Module Name | Slug | Key | Depends On | Requirements | Status |
| :---: | :--- | :--- | :--- | :--- | :--- | :---: |
{rows}
"""


def module_files() -> Dict[str, str]:
    mod = f".specify/specs/{SLUG}"
    return {
        ".specify/architecture/requirements.md": REQUIREMENTS_MD,
        ".specify/architecture/module-decomposition.md": roadmap_md(),
        f"{mod}/spec.md": spec_md(),
        f"{mod}/plan.md": "# Plan\n\nSingle slice.\n",
        f"{mod}/tasks.md": tasks_md(),
        f"{mod}/review-log.md": "# Review Log\n\n## Iteration 1\n- clean\n\nSTATUS: APPROVED\n",
        "src/link.py": "def shorten(url):\n    return url[-6:]\n",
        "tests/test_link.py": test_py(),
    }


def config_json(test_cmd: Optional[str], **extra) -> str:
    cfg = {"base_branch": "main", "commands": {"lint": None, "build": None, "test": test_cmd}}
    cfg.update(extra)
    return json.dumps(cfg, indent=2)


class TempRootCase(unittest.TestCase):
    """Gives each test an empty temporary project root."""

    def setUp(self) -> None:
        self.root = Path(tempfile.mkdtemp(prefix="sdd-toolkit-")).resolve()
        self.addCleanup(rmtree, self.root)

    def write(self, rel: str, text: str) -> Path:
        return write(self.root, rel, text)

    def write_module(self, **overrides: str) -> None:
        files = module_files()
        files.update(overrides)
        for rel, text in files.items():
            if text is not None:
                self.write(rel, text)

    def ws(self):
        return engine.Workspace(self.root)


def git(root: Path, *args: str) -> str:
    res = subprocess.run(["git", *args], cwd=root, capture_output=True, text=True)
    if res.returncode != 0:
        raise AssertionError(f"git {' '.join(args)} failed: {res.stdout}{res.stderr}")
    return res.stdout


def init_repo(root: Path) -> None:
    git(root, "init", "-q")
    git(root, "symbolic-ref", "HEAD", "refs/heads/main")
    # Local config of a throwaway fixture repo only.
    for key, value in (("user.email", "toolkit-tests@example.invalid"), ("user.name", "Toolkit Tests"),
                       ("core.autocrlf", "false"), ("commit.gpgsign", "false")):
        git(root, "config", key, value)


def commit_all(root: Path, message: str) -> None:
    git(root, "add", "-A")
    git(root, "commit", "-q", "-m", message)
