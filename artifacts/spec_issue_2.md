# Specification: Issue #2 — feat(security): Add Password-Protected URLs and Max Click Limit with auto-expiration

## 1. Problem Statement & Context
## 📌 Problem Statement
For sensitive, private, or single-use sharing (e.g. sharing temporary credentials, private documents, or limited-admission invite links), users need:
1. **Password Protection**: Restrict redirection unless a valid passkey/password is provided.
2. **Max Click / Visit Limit**: Automatically expire and disable the short URL once it reaches a configured maximum number of visits ($N$ clicks).

---

## 🎯 Proposed Solution & Scope

1. **Model & DTO Extensions (`UrlModels.cs`)**:
   - Update `CreateUrlRequest` to accept:
     - `Password` (optional string): Argon2id / SHA-256 hashed password with cryptographic salt.
     - `MaxVisits` (optional integer $\ge 1$): Maximum allowed visits before auto-destruct.
   - Update `UrlRecord` with `PasswordHash`, `PasswordSalt`, `MaxVisits`, and computed property `IsExhausted => MaxVisits.HasValue && VisitCount >= MaxVisits.Value`.
   - Update `UrlStatsResponse` with `MaxVisits`, `IsPasswordProtected` (boolean), and `IsExhausted`.

2. **Access Control & Verification Endpoints**:
   - `POST /api/v1/urls/{shortCode}/verify-access`:
     - Accepts `{ "password": "..." }`.
     - Validates the password hash; returns short-lived token or redirect URL on success (HTTP 200), or HTTP 401 Unauthorized on mismatch.
   - `GET /{shortCode}`:
     - If password protected, return HTTP 401 Unauthorized with header `WWW-Authenticate: Basic realm="NanoLink Protected URL"` or JSON redirect to unlock portal.
     - If `IsExhausted` or `IsExpired`, return HTTP 410 Gone / HTTP 404 Not Found.

3. **Storage & Concurrency Safety**:
   - Update `IUrlRepository`, `InMemoryUrlRepository`, and `SqliteUrlRepository` to enforce atomic increment and verify visit caps during redirection.

4. **Testing Suite (`tests/NanoLink.Tests/PasswordAndLimitTests.cs`)**:
   - Verify correct password hashing and verification.
   - Verify rejection when incorrect password is provided.
   - Verify URL becomes inaccessible (HTTP 404 / 410) immediately when `VisitCount >= MaxVisits`.

---

## ✅ Acceptance Criteria

- [ ] **AC 1 (Password Creation & Salting)**: Passwords are never stored in plain text; SHA-256 / HMAC with unique salt is enforced.
- [ ] **AC 2 (Protected Redirection)**: Navigating to a password-protected short URL without valid credentials returns HTTP 401 Unauthorized.
- [ ] **AC 3 (Max Visit Limit & Auto-Destruct)**: When `MaxVisits` (e.g., 3) is reached, subsequent requests return HTTP 404 / 410 and `IsExhausted` is `true`.
- [ ] **AC 4 (Analytics Exposure)**: `GET /api/v1/urls/{shortCode}` exposes `isPasswordProtected: true/false` and `maxVisits` without leaking the password hash.
- [ ] **AC 5 (Test Suite)**: 100% test pass rate across xUnit unit and integration tests.

---

## 🛡️ Non-Functional Requirements
- **Security**: Timing-attack resistant byte comparison for password verification (`CryptographicOperations.FixedTimeEquals`).
- **Performance**: Sub-millisecond hash validation overhead.

## 2. Formalized Acceptance Criteria
- [x] **AC 1 (Functional Invariant)**: Ingested feature requirements for `feat(security): Add Password-Protected URLs and Max Click Limit with auto-expiration` parsed and scoped.
- [x] **AC 2 (Interface & Contracts)**: Domain interfaces and minimal API endpoints designed to satisfy specifications.
- [x] **AC 3 (Backward Compatibility)**: Zero breaking changes to existing endpoints or models.
- [x] **AC 4 (Automated Validation)**: 100% test coverage across functional unit tests and integration tests.

## 3. Non-Functional Requirements
- High-concurrency thread safety (ConcurrentDictionary / EF Core WAL).
- Sub-millisecond latency for URL redirection endpoints.
- Zero external infrastructure dependency for testing.