---
applyTo: "tests/**"
description: "Testing rules: which project, traits, snapshots, no weakening of tests."
---
# Tests

Follow `tests/AGENTS.md`. In short:

- Domain and validator unit tests go to `tests/AgentShop.Modules.<Module>.Tests` (no database).
- HTTP behavior goes to `tests/AgentShop.Api.IntegrationTests` with `[Trait("Category", "Integration")]` on the class,
  deriving from `IntegrationTest` and using test-side DTOs from `Infrastructure/ApiClient.cs`.
- Fast loop while coding: `dotnet test --solution AgentShop.slnx -- --filter-not-trait "Category=Integration"`.
- Never accept a snapshot change without reviewing the `*.received.json` diff. Never delete or loosen a failing test to get green.
