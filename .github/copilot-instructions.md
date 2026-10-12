# Copilot instructions

Follow `AGENTS.md` in the repository root and the nearest `AGENTS.md` in `src/Modules/` or `tests/`.

When reviewing pull requests, focus on:
- references from one module to another module's internal project or types (only `*.Contracts` is allowed),
- business rules placed in handlers or endpoints instead of the aggregate in `Domain/`,
- edits of existing migrations, `AgentShop.sln`, or guardrail files (`.editorconfig`, `Directory.*`, `BannedSymbols.txt`, architecture tests),
- changes of `*.approved.json` snapshots (especially the OpenAPI contract) without an explicit request,
- new NuGet packages without justification, and integration event handlers that are not idempotent.
