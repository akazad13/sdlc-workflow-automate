# Specification: Issue #101 — Implement TTL URL Expiration, Rate Limiting Middleware, and Real-Time Analytics

## 1. Problem Statement
NanoLink currently provides basic URL redirection. High-traffic production requirements necessitate:
- Preventing database / memory clutter via Time-To-Live (TTL) URL expiration and background eviction.
- Preventing denial-of-service (DoS) attacks via IP-based Token-Bucket Rate Limiting (10 req/min).
- Exposing real-time analytics for active vs expired URLs and total visits via `/api/v1/stats`.

## 2. Acceptance Criteria
1. **TTL Expiration**:
   - `POST /api/v1/urls` accepts optional `ttlSeconds` (integer > 0).
   - Expired URLs return HTTP 404 on redirect attempt `GET /{shortCode}`.
   - Expired URLs are excluded from active counts and purged by `UrlCleanupBackgroundService`.
2. **Token-Bucket Rate Limiting**:
   - Enforce 10 req/min limit per client IP with burst capacity of 10.
   - Exhausted limit returns HTTP 429 Too Many Requests with `Retry-After` header.
   - Every response includes `X-RateLimit-Limit` and `X-RateLimit-Remaining` headers.
3. **Analytics API**:
   - `GET /api/v1/stats` returns `totalUrls`, `activeUrls`, `expiredUrls`, and `totalVisits`.
   - `GET /api/v1/urls/{shortCode}` returns detailed stats per short URL (visit count, last accessed timestamp).

## 3. Non-Functional Requirements
- High-concurrency thread safety (ConcurrentDictionary + atomic updates).
- Sub-millisecond latency for redirection endpoints.
- Zero external infrastructure dependency for testing (In-Memory + WebApplicationFactory).