# Specification: Issue #6 — feat(qrcode): Add dynamic SVG and PNG QR Code generation endpoint for shortened URLs

**Solution:** `NanoLink`  
**Target Branch:** `feat/issue-6-featqrcode-add-dynamic-svg-and`

## 1. Problem Statement & Context
## 📌 Problem Statement
Users frequently need QR codes for print media, product packaging, badges, and marketing collaterals that point directly to their shortened URLs.

Currently, `NanoLink.Api` only provides plain text redirection URLs. We need a high-performance **QR Code Generation Endpoint** (`/api/v1/urls/{shortCode}/qrcode`) that dynamically generates and streams clean SVG or PNG QR codes with configurable sizes and error correction levels.

---

## 🎯 Proposed Solution & Scope

1. **QR Code Generation Service (`src/NanoLink.Api/Services/IQrCodeService.cs`)**:
   - Create `IQrCodeService` and `QrCodeService` using lightweight matrix generation or `QRCoder` NuGet library.
   - Support rendering modes:
     - `SVG`: Scalable vector graphics with `Content-Type: image/svg+xml`.
     - `PNG`: Byte stream raster image with `Content-Type: image/png`.
   - Support query parameters:
     - `format` (optional, default `"svg"`): `"svg"` or `"png"`.
     - `size` (optional, integer 64 to 1024, default `256`): Pixel dimensions for raster rendering.
     - `ecc` (optional, default `"M"`): Error Correction Level (`"L"`, `"M"`, `"Q"`, `"H"`).

2. **API Endpoint (`src/NanoLink.Api/Program.cs`)**:
   - `GET /api/v1/urls/{shortCode}/qrcode`:
     - Checks if `shortCode` exists and is not expired (returns HTTP 404 if invalid/expired).
     - Resolves the absolute short URL (e.g. `https://nano.link/{shortCode}`).
     - Generates and streams the QR code image payload with proper `Content-Type` and `Cache-Control: public, max-age=86400` headers.

3. **Analytics Integration**:
   - Optionally track QR code requests separately or increment metadata in `UrlStatsResponse`.

4. **Testing Suite (`tests/NanoLink.Tests/QrCodeTests.cs`)**:
   - Unit test QR code generation for SVG (verifying `<svg` XML structure) and PNG (verifying PNG header magic bytes `0x89 0x50 0x4E 0x47`).
   - Integration test verifying `GET /api/v1/urls/{shortCode}/qrcode` returns HTTP 200 with correct image content types.
   - Verify HTTP 404 returned for non-existent or expired short codes.

---

## ✅ Acceptance Criteria

- [ ] **AC 1 (SVG QR Streaming)**: `GET /api/v1/urls/{shortCode}/qrcode?format=svg` returns HTTP 200 with `Content-Type: image/svg+xml` and valid SVG XML payload.
- [ ] **AC 2 (PNG QR Streaming)**: `GET /api/v1/urls/{shortCode}/qrcode?format=png&size=512` returns HTTP 200 with `Content-Type: image/png` and binary image bytes.
- [ ] **AC 3 (Validation & Expiration Handling)**: Returns HTTP 404 if the short URL does not exist or has expired.
- [ ] **AC 4 (Caching Headers)**: Responses include `Cache-Control: public, max-age=86400` for client/CDN performance caching.
- [ ] **AC 5 (Test Suite Passing)**: 100% test pass rate in `dotnet test`.

---

## 🛡️ Non-Functional Requirements
- **Performance**: Zero-allocation / stream-based generation with sub-10ms response times.
- **Security**: Strict size limits (max 1024px) to prevent memory allocation denial-of-service.

## 2. Affected Modules & Architecture Scope
- `NanoLink.Api` (Core Module)
- `NanoLink.Orchestrator` (Core Module)
- `NanoLink.Tests` (Test Suite)

## 3. Formalized Acceptance Criteria
- [x] **AC 1 (Functional Invariants)**: Feature requirements for `feat(qrcode): Add dynamic SVG and PNG QR Code generation endpoint for shortened URLs` parsed and mapped to domain models and services.
- [x] **AC 2 (Interface & Contracts)**: Domain interfaces and endpoint contracts satisfy business specifications.
- [x] **AC 3 (Backward Compatibility)**: Existing public APIs, schemas, and endpoints maintain zero breaking regressions.
- [x] **AC 4 (Automated Validation)**: 100% test coverage across all unit and integration test matrices.

## 4. Non-Functional Invariants
- High-concurrency thread safety and atomic state management.
- Strict input sanitization and defense-in-depth security best practices.
- Non-blocking asynchronous I/O with `CancellationToken` propagation.