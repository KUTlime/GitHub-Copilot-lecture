---
applyTo: "**/*.cs"
description: "C# conventions for AgentShop production and test code."
---
# C# conventions

- File-scoped namespaces matching the folder; `using` directives outside the namespace.
- `internal` by default. In a module project only `<Module>Module` is public; public API lives in `*.Contracts`.
- No `DateTime.Now/UtcNow` (inject `TimeProvider`), no `Guid.NewGuid()` (use `XxxId.New()`), no `.Result`/`.Wait()`,
  no `Newtonsoft.Json`. These are banned by `BannedSymbols.txt` and fail the build.
- Every async method takes and forwards a `CancellationToken`.
- Log with message templates, never string interpolation. For repeated log calls prefer `[LoggerMessage]` (see the integration event handlers).
- Prefer records for DTOs and events, `sealed` classes, collection expressions and primary constructors where they read well.
- Do not add interfaces or abstractions that have a single implementation and are not used across a module boundary.
- Do not suppress analyzer warnings. If a rule is genuinely wrong for a case, ask; do not edit `.editorconfig` on your own.
