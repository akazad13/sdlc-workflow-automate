# Implementation Plan: Issue #101

## 1. Component Architecture
- **`NanoLink.Api/Models/UrlModels.cs`**:
  - Updated `CreateUrlRequest`, `UrlResponse`, `UrlStatsResponse`, `GlobalStatsResponse`, `UrlRecord`.
- **`NanoLink.Api/Storage/IUrlRepository.cs`**:
  - Thread-safe repository contract with `CleanupExpiredAsync`, `GetGlobalStatsAsync`, `DeleteAsync`.
- **`NanoLink.Api/Services/TokenBucketRateLimiter.cs`**:
  - Thread-safe token bucket algorithm tracking tokens and refill timestamps per client IP.
- **`NanoLink.Api/Middleware/RateLimitingMiddleware.cs`**:
  - ASP.NET Core middleware injecting headers and enforcing 429 status code on quota exhaustion.
- **`NanoLink.Api/Services/UrlCleanupBackgroundService.cs`**:
  - `BackgroundService` executing periodic eviction loop.

## 2. Test Verification Matrix
| Test Class | Scope | Scenario |
| :--- | :--- | :--- |
| `UrlShortenerTests` | Unit | Base62 generation, custom alias validation, collision retry, TTL calculation |
| `RateLimiterTests` | Unit | Token acquisition, exhaustion, per-IP isolation, bucket reset |
| `ExpirationTests` | Unit | Expired URL blocking, background cleanup eviction verification |
| `IntegrationTests` | Integration | End-to-end WebApplicationFactory tests covering all HTTP status codes |

## 3. Git Branching Strategy
- Target Branch: `main`
- Working Branch: `feat/issue-101-ttl-ratelimit-stats`