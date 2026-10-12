---
name: add-feature-slice
description: Add a new use case (HTTP endpoint or integration event handler) to an existing AgentShop module as a vertical slice, including validation, tests and OpenAPI snapshot. Use when asked to add an endpoint, command, query or feature to Catalog, Orders or Shipping.
---

# Add a feature slice

Reference implementation: `src/Modules/Orders/AgentShop.Modules.Orders/Features/PlaceOrder/PlaceOrder.cs`.

1. **Plan.** Name the slice `<Verb><Noun>` (e.g. `RenameProduct`). Decide the module, route (relative to the module group),
   status codes, and whether it changes the aggregate, reads another module (via its `*.Contracts` interface) or publishes an event.
2. **Domain first.** Put new business rules into the aggregate in `Domain/` and add errors to `<Module>Errors`.
   Add unit tests in `tests/AgentShop.Modules.<Module>.Tests/Domain/` and run them:
   `dotnet test --project tests/AgentShop.Modules.<Module>.Tests`
3. **Slice file.** Create `Features/<Slice>/<Slice>.cs` with the same structure as `PlaceOrder`:
   `Request`/`Response` records, `Validator` (if there is input), static `HandleAsync` returning `Result`/`Result<T>`,
   nested `Endpoint : IEndpoint` with `.AddEndpointFilter<ValidationFilter<Request>>()`, `.WithName(nameof(<Slice>))`
   and `Produces*` for each status code. Endpoints and validators are discovered automatically.
4. **Event handler?** Implement `IIntegrationEventHandler<TEvent>` in the slice folder, make it idempotent, and register it
   explicitly in `<Module>Module.Add<Module>Module`.
5. **Persistence change?** Follow the `add-ef-migration` skill.
6. **Integration tests.** Add at least one happy-path and one failure test to
   `tests/AgentShop.Api.IntegrationTests/<Module>/<Module>EndpointsTests.cs` (test-side DTOs only).
7. **Contract snapshot.** Run the integration tests. `OpenApiContractTests` will fail because the API grew. Review
   `tests/AgentShop.Api.IntegrationTests/Snapshots/*.received.json`; if the diff is exactly the new endpoint, accept it:
   `$env:AGENTSHOP_ACCEPT_SNAPSHOTS='1'; dotnet test --project tests/AgentShop.Api.IntegrationTests; Remove-Item Env:AGENTSHOP_ACCEPT_SNAPSHOTS`
8. **Done.** `pwsh eng/verify.ps1` must be green (architecture tests check slice isolation and visibility).
