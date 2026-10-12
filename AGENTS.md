# AGENTS.md

## System

AgentShop is a sample ASP.NET Core Web API (.NET 10) built as a **modular monolith with vertical slices**.
Modules: `Catalog`, `Orders`, `Shipping` in `src/Modules/<Module>/`. Each module has an internal project
(`AgentShop.Modules.<Module>`) and a public contract project (`AgentShop.Modules.<Module>.Contracts`).
`src/AgentShop.Api` only composes modules; `src/AgentShop.AppHost` (Aspire) runs the API with PostgreSQL.

- Golden reference slice: `src/Modules/Orders/AgentShop.Modules.Orders/Features/PlaceOrder/PlaceOrder.cs`. Copy its shape.
- Generated, never edit by hand: `**/Persistence/Migrations/*` (use `dotnet ef`), `AgentShop.sln` (generated from `AgentShop.slnx`).
- Module-specific rules: `src/Modules/AGENTS.md`. Test rules: `tests/AGENTS.md`.

## Build & validate (verified commands, run from the repository root)

Two solution files live in the root, so ALWAYS pass `AgentShop.slnx` (or a project) to `dotnet` commands.

| Task | Command |
|---|---|
| Restore tools (dotnet-ef) | `dotnet tool restore` |
| Build (warnings are errors) | `dotnet build AgentShop.slnx` |
| Fast tests (no Docker) | `dotnet test --solution AgentShop.slnx -- --filter-not-trait "Category=Integration"` |
| All tests (needs Docker) | `dotnet test --solution AgentShop.slnx` |
| One test project | `dotnet test --project tests/AgentShop.ArchitectureTests` |
| Format check | `dotnet format AgentShop.slnx --verify-no-changes` |
| Regenerate / check `AgentShop.sln` | `dotnet run eng/sync-solutions.cs` / `dotnet run eng/sync-solutions.cs -- --check` |
| Run the app (Aspire, needs Docker) | `dotnet run --project src/AgentShop.AppHost` |

## Definition of done

`pwsh eng/verify.ps1` is green: solutions in sync, both solutions build with zero warnings, format clean,
all tests pass. Without Docker run `pwsh eng/verify.ps1 -SkipIntegration` and say explicitly that
integration tests did not run. Do not claim more than you verified.

## Decisions (local conventions)

- Minimal APIs only (no controllers). No mediator library: endpoints call the slice's static `HandleAsync`.
- One slice = one file `Features/<Verb><Noun>/<Verb><Noun>.cs` with nested `Request`, `Response`, `Validator`, `Endpoint`.
- Slice routes are relative; `XxxModule.MapXxxModule()` maps them under the module's route group.
- Expected failures return `Result`/`Result<T>` with an `Error`; exceptions are for unexpected failures only.
- Field-level failures use `ValidationError` so they have the same 400 shape as FluentValidation errors.
- Business rules live in the aggregate (`Domain/`), not in handlers. Handlers use the module's `DbContext` directly.
- Strongly-typed ids (`readonly record struct XxxId`) inside a module; plain `Guid` on HTTP and in Contracts.
- Time only via `TimeProvider`; new ids only via `XxxId.New()` (Guid v7). Pass `CancellationToken` everywhere.
- Everything in a module project is `internal` except the `XxxModule` class.
- Modules talk only through `*.Contracts`: sync via the module interface (e.g. `ICatalogModule`),
  async via integration events published after `SaveChangesAsync`. Handlers are registered explicitly in `AddXxxModule`.
- One PostgreSQL database, one schema and one `DbContext` per module. Uniqueness is enforced by DB constraints.
- Tests: xUnit v3 on Microsoft.Testing.Platform, no assertion library. JSON System.Text.Json only.

## Hard constraints

- Do NOT change `.editorconfig`, `Directory.Build.props`, `Directory.Build.targets`, `Directory.Packages.props`,
  `BannedSymbols.txt`, `global.json`, `nuget.config` or anything in `tests/AgentShop.ArchitectureTests` unless explicitly asked.
  Fix the code instead of weakening a rule or adding `#pragma warning disable`.
- Do NOT add a NuGet package without stating why in the PR. Verify it exists (NuGet MCP) and check its license.
- Never edit an existing migration. Add a new one (see the `add-ef-migration` skill) and review its SQL.
- Never accept a changed snapshot (`AGENTSHOP_ACCEPT_SNAPSHOTS=1`) without reviewing the diff. A change of
  `OpenApiContractTests.*.approved.json` is a public API change and needs an explicit request.
- Add new projects only to `AgentShop.slnx`, then run `dotnet run eng/sync-solutions.cs`.
- Never log personal data beyond what existing log messages already contain.

## Where to look

- Architecture overview and module map: `docs/architecture.md`; decisions: `docs/adr/`.
- Step-by-step skills: `.github/skills/` (`add-feature-slice`, `add-ef-migration`, `add-module`).
- Path-specific rules: `.github/instructions/*.instructions.md`.
