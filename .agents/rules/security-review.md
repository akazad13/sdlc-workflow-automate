# Security & Quality Gate Standards

## 1. Input Sanitization & Anti-SSRF
- Always validate URI schemes to permit only `http` and `https`.
- Reject dangerous schemes (`javascript:`, `data:`, `file:`, `ftp:`).
- Limit URL length to 2048 characters to prevent buffer and memory exhaustion.

## 2. Rate Limiting & DoS Protection
- Apply Token-Bucket or Leaky-Bucket Rate Limiting (e.g., 10 req/min per IP) to all public-facing endpoints.
- Return standard HTTP `429 Too Many Requests` status code with `Retry-After`, `X-RateLimit-Limit`, and `X-RateLimit-Remaining` headers.

## 3. Resource Cleanup & Memory Safety
- Ensure any TTL or cache records have background workers (`BackgroundService`) configured to evict stale entries.
