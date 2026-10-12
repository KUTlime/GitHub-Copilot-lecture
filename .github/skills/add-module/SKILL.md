---
name: add-module
description: Add a new business module (bounded context) to the AgentShop modular monolith, including Contracts project, persistence schema, registration, architecture tests and test project. Use when asked to add a new module or bounded context.
---

# Add a module

Use an existing module (e.g. `src/Modules/Shipping`) as the template. `<Module>` is PascalCase, e.g. `Payments`.

1. **Projects** (names are enforced by `Directory.Build.targets`):
   - `src/Modules/<Module>/AgentShop.Modules.<Module>.Contracts/` – references only `AgentShop.SharedKernel`.
   - `src/Modules/<Module>/AgentShop.Modules.<Module>/` – copy the `.csproj` of Shipping; reference SharedKernel.AspNetCore,
     its own Contracts and only the `*.Contracts` projects of other modules it needs.
2. **Solutions:** `dotnet sln AgentShop.slnx add <both csproj> --solution-folder src/Modules/<Module>`, then `dotnet run eng/sync-solutions.cs`.
3. **Module class:** `public static class <Module>Module` with `Add<Module>Module`, `Map<Module>Module` (route group + tag)
   and `Migrate<Module>ModuleAsync`. Everything else is `internal`.
4. **Persistence:** `<Module>DbContext` with `Schema = "<module>"`, `MigrationsHistoryTable`, a design-time factory and the
   first migration (skill `add-ef-migration`).
5. **Composition:** call `Add<Module>Module`, `Migrate<Module>ModuleAsync` and `Map<Module>Module` in `src/AgentShop.Api/Program.cs`.
6. **Tests:**
   - `tests/AgentShop.Modules.<Module>.Tests/` (copy the `.csproj` of Shipping.Tests); add it to `AgentShop.slnx`.
   - Add the module to `tests/AgentShop.ArchitectureTests/Solution.cs` (`Modules` list). This is the one allowed edit of the architecture tests.
   - Add the schema to `Schemas` in `tests/AgentShop.Api.IntegrationTests/Infrastructure/AgentShopApiFactory.cs`.
7. **Docs:** update the module map in `docs/architecture.md`; add an ADR if the module introduces a new pattern.
8. **Done:** `pwsh eng/verify.ps1` is green.
