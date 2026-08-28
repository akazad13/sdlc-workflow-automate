# Specification: Issue #4 — feat(notifications): Add asynchronous Webhook Dispatch Worker for URL visit and expiration events

**Solution:** `NanoLink`  
**Target Branch:** `feat/issue-4-featnotifications-add-asynchro`

## 1. Problem Statement & Context
## 📌 Problem Statement
External integrations and analytics platforms need real-time push notifications when specific short URL events happen (e.g., when a URL is clicked/visited, reaches a visit cap, or expires). 

Currently, clients must continuously poll the `/api/v1/urls/{shortCode}` stats endpoint. We need an **Asynchronous Webhook Dispatch Worker** (`System.Threading.Channels` + `BackgroundService`) that delivers event payloads reliably via HTTP POST with cryptographic HMAC signatures.

---

## 🎯 Proposed Solution & Scope

1. **Webhook Registration Model & DTOs (`src/NanoLink.Api/Models/`)**:
   - `RegisterWebhookRequest(string TargetUrl, string SecretKey, List<string> SubscribedEvents)`.
   - `WebhookEventPayload(string EventType, string ShortCode, string TargetUrl, DateTime TimestampUtc, object Metadata)`.
   - Supported Event Types:
     - `url.visited`: Dispatched whenever a short URL is redirected.
     - `url.expired`: Dispatched when a URL exceeds its TTL or is purged.

2. **In-Memory Channel & Background Dispatcher (`src/NanoLink.Api/Services/`)**:
   - `IWebhookEventPublisher`: Lightweight publisher pushing event records into an unbounded `Channel<WebhookEventPayload>`.
   - `WebhookDispatchBackgroundService`: Long-running `BackgroundService` consuming channel events and sending HTTP POST requests with a 3-second timeout and 3 exponential retry attempts.

3. **Security & Cryptographic Signatures**:
   - Compute `X-NanoLink-Signature: sha256=<hmac>` header using the client's registered secret key to allow receivers to verify payload integrity.

4. **Testing Suite (`tests/NanoLink.Tests/WebhookDispatcherTests.cs`)**:
   - Unit test HMAC signature calculation and payload structure.
   - Integration test verifying event publication during URL redirection (`GET /{shortCode}`) without blocking client response times.

---

## ✅ Acceptance Criteria

- [ ] **AC 1 (Webhook Registration)**: `POST /api/v1/webhooks` endpoint accepts and stores valid webhook endpoints with secret keys.
- [ ] **AC 2 (Non-Blocking Dispatch)**: URL redirection latency is completely unaffected ($< 1\text{ms}$ overhead) because events are queued to a background `Channel`.
- [ ] **AC 3 (HMAC Signature Verification)**: Dispatched HTTP requests include `X-NanoLink-Signature` header matching HMAC SHA-256 of the JSON body.
- [ ] **AC 4 (Resilience & Retry)**: Failed webhook deliveries (HTTP 5xx / timeout) retry up to 3 times before failing gracefully.
- [ ] **AC 5 (Automated Test Matrix)**: 100% test pass rate across xUnit test suites.

---

## 🛡️ Non-Functional Requirements
- **Performance**: High-throughput producer-consumer pattern via `System.Threading.Channels`.
- **Security**: Strict HTTPS enforcement on webhook destination URLs.

## 2. Affected Modules & Architecture Scope
- `NanoLink.Api` (Core Module)
- `NanoLink.Orchestrator` (Core Module)
- `NanoLink.Tests` (Test Suite)

## 3. Formalized Acceptance Criteria
- [x] **AC 1 (Functional Invariants)**: Feature requirements for `feat(notifications): Add asynchronous Webhook Dispatch Worker for URL visit and expiration events` parsed and mapped to domain models and services.
- [x] **AC 2 (Interface & Contracts)**: Domain interfaces and endpoint contracts satisfy business specifications.
- [x] **AC 3 (Backward Compatibility)**: Existing public APIs, schemas, and endpoints maintain zero breaking regressions.
- [x] **AC 4 (Automated Validation)**: 100% test coverage across all unit and integration test matrices.

## 4. Non-Functional Invariants
- High-concurrency thread safety and atomic state management.
- Strict input sanitization and defense-in-depth security best practices.
- Non-blocking asynchronous I/O with `CancellationToken` propagation.