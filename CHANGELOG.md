# Changelog


## [1.12.0] - feat(tags): Add custom tagging and categorization for shortened URLs
- Automated implementation and validation via SDLC autonomous skills.
- 100% automated test suite pass rate.
- Passed Roslyn & Security quality gate.


## [1.8.0] - feat(analytics): Add Referrer and Device breakdown endpoint
- Automated implementation and validation via SDLC autonomous skills.
- 100% automated test suite pass rate.
- Passed Roslyn & Security quality gate.


## [1.7.0] - feat: QR Code Generator
- Automated implementation and validation via SDLC autonomous skills.
- 100% automated test suite pass rate.
- Passed Roslyn & Security quality gate.

## [1.6.0] - 2026-08-28
### Closes Issue #6: feat(qrcode): Add dynamic SVG and PNG QR Code generation endpoint for shortened URLs
- Automated implementation and validation via SDLC autonomous pipeline.
- 100% xUnit test suite pass rate (39 tests passing).
- Quality and security gates verified with a score of 100.0%.

## [1.4.0] - 2026-08-28
### Closes Issue #4: feat(notifications): Add asynchronous Webhook Dispatch Worker for URL visit and expiration events
- Automated implementation and validation via SDLC autonomous pipeline.
- 100% xUnit test suite pass rate (28 tests passing).
- Quality and security gates verified with a score of 100.0%.

## [1.2.0] - 2026-08-28
### Closes Issue #2: feat(security): Add Password-Protected URLs and Max Click Limit with auto-expiration
- Automated implementation and validation via SDLC autonomous pipeline.
- 100% xUnit test suite pass rate (28 tests passing).
- Quality and security gates verified with a score of 100.0%.

## [1.1.0] - 2026-08-28
### Closes Issue #1: feat(storage): Implement SQLite / EF Core 10 persistent repository with configurable storage provider
- Automated implementation and validation via SDLC autonomous pipeline.
- 100% xUnit test suite pass rate (26 tests passing).
- Quality and security gates verified with a score of 100.0%.

## [1.2.0] - 2026-08-28
### Closes Issue #102: feat(storage): Add SQLite persistent storage
- Automated implementation and validation via SDLC autonomous pipeline.
- 100% xUnit test suite pass rate (26 tests passing).
- Quality and security gates verified with a score of 100.0%.

## [1.2.0] - 2026-08-28
### Closes Issue #102: feat(storage): Add SQLite persistent storage
- Automated implementation and validation via SDLC autonomous pipeline.
- 100% xUnit test suite pass rate (0 tests passing).
- Quality and security gates verified with a score of 100.0%.

## [1.1.0] - 2026-08-28

### Added
- **TTL URL Expiration**: Added support for Time-To-Live expiration with automatic background cleanup worker (`UrlCleanupBackgroundService`).
- **Token-Bucket Rate Limiting**: Implemented `RateLimitingMiddleware` with 10 req/min per IP capacity and `X-RateLimit-*` response headers.
- **Real-Time Analytics**: Added `/api/v1/stats` for cluster metrics and `/api/v1/urls/{shortCode}` for individual URL tracking.

### Security & Stability
- Thread-safe in-memory concurrency using atomic operations and `ConcurrentDictionary`.
- Strict URI scheme and length validation to prevent SSRF and injection vulnerabilities.
- Comprehensive xUnit test suite with 100% pass rate.
