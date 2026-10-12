# ADR 0005: One database, one schema and DbContext per module

- Status: Accepted
- Date: 2026-10-11

## Context

Modules must own their data. Separate databases per module add operational cost; shared tables create hidden coupling.

## Decision

- One PostgreSQL database (`agentshop`), one schema per module (`catalog`, `orders`, `shipping`).
- Each module has its own `DbContext` with `HasDefaultSchema`, its own `__EFMigrationsHistory` table in that schema,
  its own migrations and a design-time factory so `dotnet ef` works without starting the host.
- Registration: `AddDbContext` + `UseNpgsql(..., MigrationsHistoryTable(...))` followed by Aspire's
  `EnrichNpgsqlDbContext<T>()` (retries, health check, telemetry). All modules read the `agentshop` connection string.
- Invariants that must hold under concurrency are named database constraints (`ux_products_sku`, `ux_shipments_order_id`).
- Aggregates with state transitions (`Order`, `Shipment`) use optimistic concurrency on PostgreSQL's `xmin` system column
  (`Property<uint>("Version").IsRowVersion()`). A `DbUpdateConcurrencyException` becomes `409 *.ConcurrentModification`;
  event handlers reload and retry. The generated migration only records itself; `xmin` already exists.
- Migrations are applied on startup **only in Development** (and therefore in integration tests).

## Consequences

- No module can join another module's tables; cross-module reads go through `*.Contracts`.
- Production deployments need an explicit migration step (e.g. `dotnet ef migrations bundle` or idempotent scripts
  from `dotnet ef migrations script --idempotent`) executed before the new version starts.
- Splitting a module into its own database later only requires a different connection string.
