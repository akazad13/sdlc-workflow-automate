# C# and .NET 10 Standards for Autonomous Agents

## 1. Language & Framework Guidelines
- Target **.NET 10** / **C# 13**.
- Always enable `<Nullable>enable</Nullable>` and handle nullable reference types explicitly.
- Prefer **Minimal APIs** for high-performance microservices and routing.
- Use **C# records** (`record`, `record struct`) for immutable DTOs, request models, and domain value objects.

## 2. Asynchronous Programming & Concurrency
- Never block on async code (`.Result`, `.Wait()`, `Thread.Sleep()`). Always use `await Task.Delay(...)` or async I/O.
- Always propagate `CancellationToken ct = default` across repository, service, and controller/endpoint methods.
- For concurrent in-memory structures, use `ConcurrentDictionary` and atomic primitives (`Interlocked`).

## 3. Error Handling & Validation
- Validate incoming inputs at the boundary before executing domain logic.
- Return structured HTTP problem details or concise JSON error responses with clear error messages.
