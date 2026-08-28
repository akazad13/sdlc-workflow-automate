# Specification: Issue #1 — feat(storage): Implement SQLite / EF Core 10 persistent repository with configurable storage provider

## 1. Problem Statement & Context
## 📌 Problem Statement
Currently, `NanoLink.Api` relies solely on `InMemoryUrlRepository`. When the service restarts or crashes, all shortened URLs, metadata, and analytics click stats are lost. 

To support persistent production workloads while keeping the fast in-memory option for unit testing and local development, we need an **EF Core 10 / SQLite** repository implementation configurable via `appsettings.json`.

---

## 🎯 Proposed Solution & Scope

1. **Entity Framework Core 10 & SQLite Integration**:
   - Add NuGet packages: `Microsoft.EntityFrameworkCore.Sqlite` (v10.x), `Microsoft.EntityFrameworkCore.Design`.
   - Create `NanoLinkDbContext` in `src/NanoLink.Api/Storage/`.
   - Configure entity mapping for `UrlRecord` (unique index on `ShortCode`, indices on `ExpiresAtUtc` and `CreatedAtUtc`).

2. **Repository Implementation**:
   - Create `SqliteUrlRepository : IUrlRepository` using `IDbContextFactory<NanoLinkDbContext>` or scoped `NanoLinkDbContext`.
   - Support all `IUrlRepository` operations:
     - `GetAsync(shortCode, recordVisit, ct)`
     - `GetStatsAsync(shortCode, ct)`
     - `CreateAsync(record, ct)`
     - `DeleteAsync(shortCode, ct)`
     - `CleanupExpiredAsync(ct)`
     - `GetGlobalStatsAsync(ct)`

3. **Storage Provider Strategy**:
   - Add configuration setting in `appsettings.json`:
     ```json
     {
       "Storage": {
         "Provider": "Sqlite", // Options: "InMemory", "Sqlite"
         "ConnectionString": "Data Source=nanolink.db"
       }
     }
     ```
   - Auto-apply EF Core migrations/`EnsureCreatedAsync` at startup if SQLite is chosen.

4. **Testing & Validation**:
   - Add unit and integration tests verifying SQLite persistence across service restarts and concurrent read/write operations.

---

## ✅ Acceptance Criteria

- [ ] **AC 1 (Configuration Switching)**: When `Storage:Provider` is set to `"Sqlite"`, `SqliteUrlRepository` is injected; when `"InMemory"`, `InMemoryUrlRepository` is injected.
- [ ] **AC 2 (Persistence across restarts)**: URLs saved to SQLite persist after restarting the API and can be resolved/redirected.
- [ ] **AC 3 (Concurrency & Thread-Safety)**: SQLite WAL (Write-Ahead Logging) mode is enabled to support high concurrent reads and writes without database lock contention.
- [ ] **AC 4 (Background Cleanup)**: `UrlCleanupBackgroundService` successfully purges expired records from the SQLite database.
- [ ] **AC 5 (Test Suite Passing)**: All existing tests + new SQLite integration tests pass with 100% success rate in `dotnet test`.

---

## 🛡️ Non-Functional Requirements
- **Performance**: URL redirection queries must execute with $< 5\text{ms}$ latency using index on `ShortCode`.
- **Database Safety**: Parameterized queries via EF Core to prevent SQL injection.
- **Resilience**: Graceful startup creation of SQLite database directory and schema.

## 2. Formalized Acceptance Criteria
- [x] **AC 1 (Functional Invariant)**: Ingested feature requirements for `feat(storage): Implement SQLite / EF Core 10 persistent repository with configurable storage provider` parsed and scoped.
- [x] **AC 2 (Interface & Contracts)**: Domain interfaces and minimal API endpoints designed to satisfy specifications.
- [x] **AC 3 (Backward Compatibility)**: Zero breaking changes to existing endpoints or models.
- [x] **AC 4 (Automated Validation)**: 100% test coverage across functional unit tests and integration tests.

## 3. Non-Functional Requirements
- High-concurrency thread safety (ConcurrentDictionary / EF Core WAL).
- Sub-millisecond latency for URL redirection endpoints.
- Zero external infrastructure dependency for testing.