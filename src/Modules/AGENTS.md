# AGENTS.md – modules

Rules for code under `src/Modules/`. The root `AGENTS.md` still applies.

## Anatomy of a module

```
src/Modules/<Module>/
├─ AgentShop.Modules.<Module>.Contracts/   public: module interface, DTOs, integration events (no EF Core, no ASP.NET Core)
└─ AgentShop.Modules.<Module>/
   ├─ <Module>Module.cs                    the ONLY public type: Add<Module>Module, Map<Module>Module, Migrate<Module>ModuleAsync
   ├─ Domain/                              aggregate, strongly-typed id, status enum, <Module>Errors
   ├─ Features/<Slice>/<Slice>.cs          one use case per folder and file
   ├─ PublicApi/                           implementation of the Contracts interface (only if the module has one)
   └─ Persistence/                         <Module>DbContext (own schema), design-time factory, configurations, Migrations/
```

## Writing a slice

Copy `Orders/AgentShop.Modules.Orders/Features/PlaceOrder/PlaceOrder.cs`:

1. `internal static class <Slice>` in namespace `AgentShop.Modules.<Module>.Features.<Slice>`.
2. Nested `public sealed record Request/Response` (effectively internal), `internal sealed class Validator`.
3. `internal static async Task<Result<Response>> HandleAsync(...)`: load → call the aggregate → `SaveChangesAsync` → publish events.
4. Nested `internal sealed class Endpoint : IEndpoint` with a RELATIVE route, `.AddEndpointFilter<ValidationFilter<Request>>()`
   when there is a validator, `.WithName(nameof(<Slice>))`, and `Produces*` for every status code it returns.
5. Endpoints and validators are discovered automatically. Integration event handlers are NOT: register them in `Add<Module>Module`.

A slice never references another slice. Share code only through `Domain/` or `Persistence/` of the same module.

## Talking to other modules

- Reference ONLY `AgentShop.Modules.<Other>.Contracts` (a `ProjectReference` to another module's internal project breaks the architecture tests).
- Synchronous reads: the other module's interface, e.g. `ICatalogModule.GetProductsAsync`.
- Reactions: implement `IIntegrationEventHandler<TEvent>`; make it idempotent (unique constraint + catch `23505`, or a state check).
- Publish integration events only after `SaveChangesAsync` succeeded.

## Errors and validation

- Shape validation (required, length, range) → the slice `Validator` (FluentValidation).
- Rules that need data (unknown product, wrong state) → return an `Error` from `<Module>Errors` or a `ValidationError`
  keyed like FluentValidation (`Lines[0].ProductId`).
- Status mapping is central (`ResultExtensions`): NotFound → 404, Conflict → 409, ValidationError → 400.

## Concurrency

- Uniqueness and idempotency: named unique constraints, handle `PostgresErrorCodes.UniqueViolation` by constraint name.
- State transitions: aggregates carry an `xmin` concurrency token. Catch `DbUpdateConcurrencyException` around
  `SaveChangesAsync` and return `<Module>Errors.ConcurrentModification` (409); event handlers reload and retry.
- After a successful commit, publish events with `CancellationToken.None` so a client disconnect cannot drop them.
