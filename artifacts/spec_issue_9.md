# Specification: Issue #9 — feat(analytics): Add Referrer and Device/Browser click analytics breakdown endpoint

## 1. Problem Statement
## 📌 Problem Statement
Marketing and operations teams need deeper insights into who is clicking shortened URLs. While we currently track total `VisitCount`, users cannot see where clicks originated (Referrer URL) or what devices/browsers (User-Agent) their visitors are using.

We need a dedicated **Click Analytics Endpoint** (`GET /api/v1/urls/{shortCode}/analytics`) that records and aggregates referrer headers and client devices.

---

## 🎯 Proposed Solution & Scope

1. **Analytics Data Models & DTOs (`src/NanoLink.Api/Models/`)**:
   - `ClickEventRecord`:
     - `string ShortCode`
     - `DateTime ClickedAtUtc`
     - `string? Referrer` (e.g., `https://twitter.com`, `direct`)
     - `string? UserAgent`
     - `string? Browser` (e.g., `Chrome`, `Firefox`, `Safari`, `Other`)
     - `string? Platform` (e.g., `Windows`, `macOS`, `Linux`, `iOS`, `Android`, `Other`)
   - `UrlAnalyticsResponse`:
     - `string ShortCode`
     - `int TotalClicks`
     - `Dictionary<string, int> TopReferrers` (top 5 referrer domains)
     - `Dictionary<string, int> DeviceBreakdown` (e.g., Mobile vs Desktop)
     - `Dictionary<string, int> BrowserBreakdown`

2. **Click Analytics Service (`src/NanoLink.Api/Services/IAnalyticsService.cs`)**:
   - `RecordClickAsync(string shortCode, string? referrer, string? userAgent)`: Asynchronously captures click metadata.
   - `GetAnalyticsAsync(string shortCode)`: Computes and returns aggregated click breakdowns.
   - Lightweight parser for User-Agent strings (identifying Chrome, Safari, Firefox, Edge, iOS, Android, Desktop).

3. **API Endpoints (`src/NanoLink.Api/Program.cs`)**:
   - `GET /api/v1/urls/{shortCode}/analytics`:
     - Returns HTTP 200 with `UrlAnalyticsResponse`.
     - Returns HTTP 404 if the short code does not exist.
   - Update `GET /{shortCode}` redirect endpoint to capture `Referer` and `User-Agent` headers and pass them to the analytics service.

4. **Testing Suite (`tests/NanoLink.Tests/AnalyticsTests.cs`)**:
   - Unit tests for User-Agent parsing and referrer domain extraction.
   - Integration tests verifying `GET /api/v1/urls/{shortCode}/analytics` returns aggregated stats after multiple simulated clicks with varied headers.

---

## ✅ Acceptance Criteria

- [ ] **AC 1 (Click Ingestion)**: Every `GET /{shortCode}` redirect captures `Referer` and `User-Agent` headers without increasing redirect latency.
- [ ] **AC 2 (Analytics Endpoint)**: `GET /api/v1/urls/{shortCode}/analytics` returns top referrers and browser/platform percentage breakdowns.
- [ ] **AC 3 (Validation)**: Returns HTTP 404 for invalid or non-existent short codes.
- [ ] **AC 4 (Automated Tests)**: 100% test pass rate across xUnit test suites.

---

## 🛡️ Non-Functional Requirements
- **Performance**: In-memory channel / non-blocking recording so click capture does not slow down HTTP 302 redirects.
- **Privacy**: No IP addresses stored in click logs to maintain GDPR compliance.


## 2. Acceptance Criteria
- [x] **AC 1**: Ingested requirement for `feat(analytics): Add Referrer and Device/Browser click analytics breakdown endpoint` parsed and mapped to domain models.
- [x] **AC 2**: Interface contracts and minimal API endpoints satisfy business specifications.
- [x] **AC 3**: 100% automated test coverage in xUnit test suites.
- [x] **AC 4**: Zero regressions to existing API contracts.
