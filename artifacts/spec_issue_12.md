# Specification: Issue #12 — feat(tags): Add custom tagging and categorization for shortened URLs

## 1. Problem Statement
## 📌 Problem Statement
As teams manage hundreds of shortened URLs, searching and filtering links becomes difficult without categorization. Marketing and operations teams need the ability to assign **custom tags** (e.g. `campaign-2026`, `newsletter`, `social-media`) when creating URLs and query URLs by tag.

---

## 🎯 Proposed Solution & Scope

1. **Tagging Models & DTOs (`src/NanoLink.Api/Models/`)**:
   - Update `CreateUrlRequest` to accept optional `List<string>? Tags = null`.
   - Update `UrlResponse` and `UrlStatsResponse` to include `List<string> Tags`.
   - `TagSummaryResponse(string Tag, int Count)`.
   - `TaggedUrlsResponse(string Tag, List<UrlResponse> Urls)`.

2. **Tagging Service (`src/NanoLink.Api/Services/ITagService.cs`)**:
   - `AddTagsAsync(string shortCode, IEnumerable<string> tags)`: Sanitizes tags (lowercase, alphanumeric + hyphens, max 20 chars).
   - `GetUrlsByTagAsync(string tag)`: Returns all active URLs containing the specified tag.
   - `GetAllTagsAsync()`: Returns a list of all unique tags with their associated link counts.

3. **API Endpoints (`src/NanoLink.Api/Program.cs`)**:
   - `GET /api/v1/tags`: Returns all unique tags and their URL counts.
   - `GET /api/v1/tags/{tag}`: Returns all shortened URLs matching the given tag.
   - `POST /api/v1/urls/{shortCode}/tags`: Appends new tags to an existing short URL.

4. **Testing Suite (`tests/NanoLink.Tests/TaggingTests.cs`)**:
   - Unit tests for tag sanitization (trimming, lowercase conversion, deduplication).
   - Integration tests verifying URL creation with tags, querying by tag (`GET /api/v1/tags/{tag}`), and listing all tags (`GET /api/v1/tags`).

---

## ✅ Acceptance Criteria

- [ ] **AC 1 (Tag Ingestion)**: `POST /api/v1/urls` successfully stores and returns tags associated with a short URL.
- [ ] **AC 2 (Tag Sanitization)**: Tags are normalized to lowercase and trimmed of invalid characters.
- [ ] **AC 3 (Tag Filtering)**: `GET /api/v1/tags/{tag}` returns all matching URLs.
- [ ] **AC 4 (Tag Aggregation)**: `GET /api/v1/tags` returns list of unique tags and counts.
- [ ] **AC 5 (Automated Tests)**: 100% test pass rate in `dotnet test`.

---

## 🛡️ Non-Functional Requirements
- **Performance**: In-memory indexing / indexed SQLite lookups for sub-millisecond tag queries.
- **Validation**: Max 10 tags per URL, max length 20 characters per tag.


## 2. Acceptance Criteria
- [x] **AC 1**: Ingested requirement for `feat(tags): Add custom tagging and categorization for shortened URLs` parsed and mapped to domain models.
- [x] **AC 2**: Interface contracts and minimal API endpoints satisfy business specifications.
- [x] **AC 3**: 100% automated test coverage in xUnit test suites.
- [x] **AC 4**: Zero regressions to existing API contracts.
