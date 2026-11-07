"""Tests validating cross-agent plugin manifests and configuration.

Ensures plugin.json, .claude-plugin, .agents/plugins/marketplace.json,
and .gemini/commands satisfy the Cross-Agent Plugin Architecture (ADR-0004).
Zero external dependencies; uses only Python standard library.
"""

from __future__ import annotations

import json
from pathlib import Path
import unittest

REPO_ROOT = Path(__file__).resolve().parents[2]


class TestPluginManifest(unittest.TestCase):
    """Validates root plugin.json, Claude Code, and Marketplace manifests."""

    def test_root_plugin_json_structure(self) -> None:
        """Root plugin.json must exist, be valid JSON, and declare required fields."""
        manifest_path = REPO_ROOT / "plugin.json"
        self.assertTrue(manifest_path.is_file(), "plugin.json must exist at repo root")

        with manifest_path.open("r", encoding="utf-8") as f:
            data = json.load(f)

        self.assertEqual(data.get("name"), "sdlc-automate-workflow")
        self.assertTrue(data.get("version"), "Version must be defined")
        self.assertTrue(data.get("description"), "Description must be defined")
        self.assertTrue(data.get("license"), "License must be defined")
        self.assertEqual(data.get("skills"), "./.agents/skills")
        self.assertIn("suggestedPrompts", data)
        self.assertIsInstance(data["suggestedPrompts"], list)
        self.assertGreaterEqual(len(data["suggestedPrompts"]), 1)

    def test_claude_plugin_manifest(self) -> None:
        """Claude plugin manifest must exist and reference skills."""
        manifest_path = REPO_ROOT / ".claude-plugin" / "plugin.json"
        self.assertTrue(manifest_path.is_file(), ".claude-plugin/plugin.json must exist")

        with manifest_path.open("r", encoding="utf-8") as f:
            data = json.load(f)

        self.assertEqual(data.get("name"), "sdlc-automate-workflow")
        self.assertEqual(data.get("skills"), "./.agents/skills")
        skills_path = (REPO_ROOT / data["skills"]).resolve()
        self.assertTrue(skills_path.is_dir(), f"Skills path {skills_path} must exist")

    def test_marketplace_manifest(self) -> None:
        """Marketplace manifest must exist and define the plugin entry."""
        manifest_path = REPO_ROOT / ".agents" / "plugins" / "marketplace.json"
        self.assertTrue(manifest_path.is_file(), ".agents/plugins/marketplace.json must exist")

        with manifest_path.open("r", encoding="utf-8") as f:
            data = json.load(f)

        self.assertEqual(data.get("name"), "sdlc-automate-workflow")
        plugins = data.get("plugins", [])
        self.assertIsInstance(plugins, list)
        self.assertTrue(any(p.get("name") == "sdlc-automate-workflow" for p in plugins))

    def test_gemini_slash_commands(self) -> None:
        """All four SDD lifecycle stages must have corresponding .toml command files."""
        commands_dir = REPO_ROOT / ".gemini" / "commands"
        self.assertTrue(commands_dir.is_dir(), ".gemini/commands must exist")

        expected_commands = ["sdd-intake.toml", "sdd-spec.toml", "sdd-loop.toml", "sdd-pr.toml"]
        for cmd_file in expected_commands:
            target = commands_dir / cmd_file
            self.assertTrue(target.is_file(), f"Command file {cmd_file} must exist in .gemini/commands")
            content = target.read_text(encoding="utf-8")
            self.assertIn("description =", content, f"{cmd_file} must define a description")
            self.assertIn("prompt =", content, f"{cmd_file} must define a prompt")

    def test_referenced_skills_have_valid_frontmatter(self) -> None:
        """All declared skills must have SKILL.md with name and description."""
        skills_dir = REPO_ROOT / ".agents" / "skills"
        self.assertTrue(skills_dir.is_dir(), ".agents/skills must exist")

        expected_skills = ["sdd-intake", "sdd-spec", "sdd-loop", "sdd-pr"]
        for skill in expected_skills:
            skill_md = skills_dir / skill / "SKILL.md"
            self.assertTrue(skill_md.is_file(), f"SKILL.md must exist for {skill}")
            text = skill_md.read_text(encoding="utf-8")
            self.assertTrue(text.startswith("---"), f"{skill}/SKILL.md must start with YAML frontmatter")
            self.assertIn("name:", text)
            self.assertIn("description:", text)


if __name__ == "__main__":
    unittest.main()
