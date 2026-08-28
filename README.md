# 🚀 Autonomous SDLC Automation Framework for .NET 10 & GitHub

An end-to-end autonomous Software Development Life Cycle (SDLC) framework for **ASP.NET Core & .NET 10 (C# 13)**, featuring self-healing CI/CD, Roslyn quality gates, automated Pull Request generation, and **Antigravity Autonomous Agents**.

---

## 🌟 6-Phase Autonomous Lifecycle Architecture

```mermaid
flowchart TD
    subgraph Intake ["Phase 1: Inception & Triage"]
        I1[GitHub Issue / Feature Request] --> T1[Skill: sdlc-triage]
        T1 --> T2[Formalized Spec & Acceptance Criteria]
    end

    subgraph Planning ["Phase 2: Architecture & Design"]
        T2 --> P1[Skill: sdlc-plan]
        P1 --> P2[C# Solution Plan & xUnit Test Matrix]
    end

    subgraph Execution ["Phase 3: Coding & TDD"]
        P2 --> C1[Skill: sdlc-coder]
        C1 --> C2[Branch feat/issue-N in ASP.NET Core]
        C2 --> C3[xUnit Test Suite & Integration Tests]
    end

    subgraph CI ["Phase 4: CI & Self-Healing Loop"]
        C3 --> CI1[dotnet test Runner]
        CI1 -- Failure --> H1[Self-Healing Diagnostic Agent]
        H1 --> CI1
        CI1 -- 100% Pass --> PR1[Create Pull Request]
    end

    subgraph Quality ["Phase 5: Code Review & Quality Gate"]
        PR1 --> R1[Skill: sdlc-reviewer]
        R1 --> R2[Roslyn Quality & Security Score: 100%]
        R2 --> M1[Auto-Approve PR]
    end

    subgraph Release ["Phase 6: Release & Verification"]
        M1 --> REL1[.csproj Version Bumping]
        REL1 --> REL2[Auto-Generate CHANGELOG.md]
        REL2 --> REL3[Health & Smoke Verification]
    end
```

---

## 📦 Project Structure

```
SDLC-automate-workflow/
├── NanoLink.slnx                           # Solution configuration
├── .agents/                               # Antigravity agent customizations (Portable)
│   ├── rules/
│   │   ├── csharp-standards.md           # C# 13 style, async/await, nullable types
│   │   └── security-review.md            # OWASP, rate limiting, URI sanitization
│   ├── skills/
│   │   ├── sdlc-triage/SKILL.md          # Spec formalization skill
│   │   ├── sdlc-plan/SKILL.md            # Architecture planning skill
│   │   ├── sdlc-ci-repair/SKILL.md       # Self-healing CI diagnostics skill
│   │   ├── sdlc-reviewer/SKILL.md        # Roslyn code review skill
│   │   └── sdlc-release/SKILL.md         # Versioning & changelog skill
│   └── hooks.json                        # Lifecycle hooks
├── .github/workflows/
│   ├── dotnet-ci.yml                     # .NET 10 Matrix CI workflow
│   └── auto-sdlc-agent.yml               # Issue trigger & automated PR pipeline
├── artifacts/                            # Automated stage outputs
│   ├── spec_issue_*.md                   # Formalized specs & acceptance criteria
│   ├── plan_issue_*.md                   # Architecture & test plans
│   └── pr_review_issue_*.md              # Roslyn code review & approval
├── src/
│   ├── NanoLink.Api/                     # High-performance Minimal API Microservice
│   │   ├── Program.cs                    # Minimal API endpoints & DI
│   │   ├── Models/UrlModels.cs           # Immutable DTO records
│   │   ├── Storage/                      # InMemory & SQLite EF Core 10 Repositories
│   │   │   ├── IUrlRepository.cs
│   │   │   ├── InMemoryUrlRepository.cs
│   │   │   ├── NanoLinkDbContext.cs      # EF Core 10 DbContext
│   │   │   └── SqliteUrlRepository.cs    # SQLite WAL implementation
│   │   ├── Services/                     # Shortener, TokenBucketRateLimiter, Cleanup
│   │   └── Middleware/                   # Token-Bucket RateLimitingMiddleware
│   └── NanoLink.Orchestrator/            # Generic Spectre.Console CLI SDLC Engine
│       ├── Program.cs                    # CLI runner (supports any .NET solution)
│       ├── Core/                         # Generic 6-phase state machine pipeline
│       └── QualityGates/                 # Roslyn evaluator & test runner
├── tests/
│   └── NanoLink.Tests/                   # xUnit test suite (28 passing tests)
│       ├── UrlShortenerTests.cs          # Base62 generation, validation, collision
│       ├── RateLimiterTests.cs           # Token bucket rate limiting tests
│       ├── ExpirationTests.cs            # TTL and background eviction tests
│       ├── SqliteRepositoryTests.cs      # SQLite database persistence tests
│       └── IntegrationTests.cs           # WebApplicationFactory end-to-end tests
├── docs/
│   └── implementation_plan.md            # Master SDLC design plan
└── CHANGELOG.md                          # Automated changelog
```

---

## ⚡ Quick Start & Commands

### 1. Build Solution
```powershell
dotnet build
```

### 2. Run Test Suite (xUnit)
```powershell
dotnet test --verbosity normal
```

### 3. Run Autonomous SDLC Orchestrator CLI
```powershell
# Run with default context:
dotnet run --project src/NanoLink.Orchestrator

# Run against a specific issue:
dotnet run --project src/NanoLink.Orchestrator -- --issue 102 --title "feat: SQLite Storage" --desc "Add SQLite persistent repository"

# Run against ANY external repository:
dotnet run --project src/NanoLink.Orchestrator -- --dir "D:/Projects/MyOtherApp" --issue 10 --title "Add Auth"
```

### 4. Run API Locally
```powershell
dotnet run --project src/NanoLink.Api
```

---

## 🌐 API Endpoints

| Method | Route | Description |
| :--- | :--- | :--- |
| `POST` | `/api/v1/urls` | Shortens a target URL with optional custom alias & TTL |
| `GET` | `/{shortCode}` | Redirects (302) to target URL (increments visit count) |
| `GET` | `/api/v1/urls/{shortCode}` | Retrieves metadata and visit statistics for a short URL |
| `DELETE`| `/api/v1/urls/{shortCode}` | Deletes a short URL |
| `GET` | `/api/v1/stats` | Cluster-wide analytics (total, active, expired, visits) |
| `GET` | `/health` | Health check endpoint (reports status, version, storage provider) |

---

## 🤖 GitHub Actions Automated Trigger

Whenever you create a GitHub issue or attach the **`sdlc-agent`** label:
1. GitHub Actions spins up the pipeline.
2. The orchestrator ingests the issue, runs triage, plans the C# architecture, and validates tests.
3. The engine creates a new branch `feat/issue-<number>-autonomous-impl`, commits the source code, pushes to GitHub, and **automatically opens a Pull Request** linked to your issue with the full test and quality review!
