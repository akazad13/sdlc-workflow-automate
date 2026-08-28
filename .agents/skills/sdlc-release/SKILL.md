---
name: sdlc-release
description: Automated SemVer version bumping, CHANGELOG generation, and deployment smoke verification.
---

# SDLC Release & Verification Skill

## Instructions
1. Bump minor version in `NanoLink.Api.csproj` / metadata.
2. Ingest closed issue details and summarize changes into `CHANGELOG.md`.
3. Verify live `/health` and `/api/v1/stats` smoke checks.
