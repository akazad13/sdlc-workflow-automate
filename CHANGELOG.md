# Changelog

## [1.1.0] - 2026-08-28

### Added
- **TTL URL Expiration**: Added support for Time-To-Live expiration with automatic background cleanup worker (`UrlCleanupBackgroundService`).
- **Token-Bucket Rate Limiting**: Implemented `RateLimitingMiddleware` with 10 req/min per IP capacity and `X-RateLimit-*` response headers.
- **Real-Time Analytics**: Added `/api/v1/stats` for cluster metrics and `/api/v1/urls/{shortCode}` for individual URL tracking.

### Security & Stability
- Thread-safe in-memory concurrency using atomic operations and `ConcurrentDictionary`.
- Strict URI scheme and length validation to prevent SSRF and injection vulnerabilities.
- Comprehensive xUnit test suite with 100% pass rate.
