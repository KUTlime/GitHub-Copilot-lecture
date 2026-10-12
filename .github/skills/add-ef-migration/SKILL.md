---
name: add-ef-migration
description: Create and review an EF Core migration for one AgentShop module (Catalog, Orders or Shipping) after changing its entities or configurations. Use whenever a change touches Domain entities persisted by EF Core or Persistence/Configurations.
---

# Add an EF Core migration

Each module has its own `DbContext`, schema and migrations folder. Run everything from the repository root.

1. Restore the local tool once: `dotnet tool restore`
2. Make the model change (entity in `Domain/`, mapping in `Persistence/Configurations/`) and build: `dotnet build AgentShop.slnx`
3. Generate the migration (example for Orders; replace module and context):
   ```
   dotnet ef migrations add <DescriptiveName> --project src/Modules/Orders/AgentShop.Modules.Orders --startup-project src/Modules/Orders/AgentShop.Modules.Orders --context OrdersDbContext --output-dir Persistence/Migrations
   ```
   The module is its own startup project thanks to the design-time factory (`<Module>DbContextFactory`); no database is needed.
4. Review the SQL. Look for `DROP`, column renames and non-nullable columns without defaults:
   ```
   dotnet ef migrations script --project src/Modules/Orders/AgentShop.Modules.Orders --startup-project src/Modules/Orders/AgentShop.Modules.Orders --context OrdersDbContext --idempotent
   ```
5. Destructive or renaming operations need an explicit request. Prefer expand/contract: add the new column, migrate data,
   remove the old column in a later release. EF may propose `RenameColumn`/`DropColumn` when it only "cannot see" a property.
6. Never edit a migration that is already on `main`; add a new one. To undo an unmerged migration:
   `dotnet ef migrations remove --project <module> --startup-project <module> --context <XxxDbContext>`
7. Run the integration tests (they apply migrations on a fresh PostgreSQL): `dotnet test --project tests/AgentShop.Api.IntegrationTests`
