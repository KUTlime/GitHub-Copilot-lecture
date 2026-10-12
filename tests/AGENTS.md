# AGENTS.md – tests

Rules for code under `tests/`. The root `AGENTS.md` still applies.

## Which test goes where

| Project | What | Docker |
|---|---|---|
| `AgentShop.Modules.<Module>.Tests` | Unit tests of aggregates and validators (`internal` types are visible to the module's own test project only) | no |
| `AgentShop.ArchitectureTests` | Executable architecture rules. Do not change them without an explicit request | no |
| `AgentShop.Api.IntegrationTests` | Black-box HTTP tests against PostgreSQL (Testcontainers). Every class has `[Trait("Category", "Integration")]` | yes |

## Conventions

- xUnit v3 + built-in `Assert`. Names: `Method_Scenario_ExpectedResult`.
- Every bug fix gets a regression test. Every new slice gets at least one happy-path and one failure integration test.
- Use `FakeTimeProvider` in unit tests. Integration tests use `Factory.Clock` (`TestClock`), reset to 2026-01-15 10:00 UTC before each test.
- Integration tests use their own DTOs (`Infrastructure/ApiClient.cs`), never production types: they guard the HTTP contract.
- Integration tests run serially and the database is reset (Respawn) before each test. Do not rely on data from other tests.
- Do not delete or weaken a failing test to make the build green. Fix the code, or explain why the test is wrong.

## Snapshot (approval) tests

`JsonSnapshot.MatchAsync(json)` compares with `AgentShop.Api.IntegrationTests/Snapshots/<Class>.<Method>.approved.json`.
GUIDs become `Guid_N` and `traceId` is removed. On a mismatch a `*.received.json` file is written (git-ignored).

1. Read the diff between `*.approved.json` and `*.received.json`.
2. Only if the change is intended: `$env:AGENTSHOP_ACCEPT_SNAPSHOTS='1'; dotnet test --project tests/AgentShop.Api.IntegrationTests` (ignored on CI).
3. Run the tests again without the variable: they must pass unchanged.

`OpenApiContractTests` snapshots the whole OpenAPI document. A diff there is a public API change.
