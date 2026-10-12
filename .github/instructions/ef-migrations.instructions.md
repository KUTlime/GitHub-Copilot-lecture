---
applyTo: "**/Persistence/**"
description: "EF Core persistence and migration rules (schema per module)."
---
# Persistence and migrations

- Each module has its own `DbContext`, its own PostgreSQL schema (`<Module>DbContext.Schema`) and its own migrations history table.
- Entity configuration lives in `Persistence/Configurations/<Entity>Configuration.cs`; strongly-typed ids use `HasConversion`.
- Invariants that must hold under concurrency (uniqueness, idempotency) are database constraints with an explicit name,
  e.g. `ux_products_sku`; handle violations by constraint name (`PostgresErrorCodes.UniqueViolation`).
- `Persistence/Migrations/*` is generated. Never edit an existing migration; create a new one with the `add-ef-migration` skill.
- Migrations must be backward compatible with the previous application version (add, then migrate data, then remove).
- Do not reference another module's `DbContext` or tables.
