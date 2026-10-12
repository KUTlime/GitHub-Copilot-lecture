---
description: 'Vytvoří vzorový "agent-ready" repozitář ASP.NET Core Web API (.NET 10, modulární monolit + vertical slices, CPM, analyzéry, architektonické testy, Aspire, AGENTS.md).'
agent: 'agent'
argument-hint: 'Cílová složka nového repozitáře (např. C:\Users\radek\source\KUTlime\AgentShop)'
---

# Úkol: Vytvoř vzorový „agent-ready" repozitář ASP.NET Core Web API

Jsi senior .NET architekt a vývojář. Vytvoř **kompletní, buildovatelný a otestovaný** repozitář ASP.NET Core Web API, který slouží jako **vzorová aplikace pro školení „vývoj .NET aplikací s AI agenty"**. Repozitář musí přesně ztělesňovat principy z dokumentu [Architektura .NET aplikací pro agentní vývoj s LLM](../../docs/dotnet-architektura-pro-agentni-vyvoj.md) – přečti si ho celý, než začneš (zejména kapitoly 3–8, 10 a 12).

**Cílová složka:** `${input:targetDir:C:\Users\radek\source\KUTlime\AgentShop}`
Pokud složka neexistuje, vytvoř ji a inicializuj v ní nový git repozitář (`git init`, výchozí větev `main`). Do jiných složek nezapisuj.

Nejde o „hello world". Jde o **referenční implementaci**, kterou budou lidé i agenti kopírovat – každý soubor musí být záměrný, konzistentní a vysvětlitelný.

---

## 1. Rozhodnutí (parametry)

Následující rozhodnutí jsou závazná. Pokud něco z toho nejde technicky splnit, **nezaměňuj potichu** – zvol nejbližší alternativu, zapiš ji do ADR a uveď ji v závěrečném reportu.

| Oblast | Rozhodnutí |
|---|---|
| Název řešení / root namespace | `AgentShop` |
| Platforma | .NET 10 (SDK pinned v `global.json`), C# `latest` |
| Architektura | **Modulární monolit**; uvnitř modulů **vertical slices** (feature folders) |
| Doména | E-shop se 3 moduly: **Catalog**, **Orders**, **Shipping** |
| Endpointy | **Minimal APIs** (ne controllery, ne FastEndpoints) |
| Dispatch | **Žádný mediator** (žádný MediatR/Wolverine/Mediator) – endpoint volá handler přímo |
| Validace | **FluentValidation** + endpoint filter |
| Chyby | Vlastní minimalistický **Result pattern** (`Result`, `Result<T>`, `Error`) + ProblemDetails |
| ID | **Strongly-typed IDs** jako `readonly record struct` uvnitř modulů; na HTTP/Contracts hranici `Guid` |
| Persistence | **EF Core + PostgreSQL**, jedna DB, **schéma + `DbContext` na modul**, migrace per modul |
| Komunikace modulů | Synchronně přes `*.Contracts` rozhraní; asynchronně přes **in-process integrační události** |
| Orchestrace | **.NET Aspire** (AppHost + ServiceDefaults), `aspire agent init` |
| API dokumentace | `Microsoft.AspNetCore.OpenApi` + **Scalar** UI (ne Swashbuckle) |
| Testy | **xUnit v3** na **Microsoft.Testing.Platform** (MTP režim `dotnet test`), bez assertion knihovny (vestavěný `Assert`) |
| Integrační testy | `WebApplicationFactory<Program>` + **Testcontainers.PostgreSql** + **Respawn** |
| Snapshot testy | **Verify** (`Verify.XunitV3`) – odpovědi API + OpenAPI dokument |
| Architektonické testy | **ArchUnitNET** (`TngTech.ArchUnitNET.xUnitV3`) |
| Analyzéry | SDK analyzéry (`AnalysisLevel`), `Microsoft.CodeAnalysis.BannedApiAnalyzers`, `Meziantou.Analyzer` |
| Balíčky | **Central Package Management** + package source mapping + NuGet Audit |
| Solution soubory | **Oba**: `AgentShop.slnx` je **jediný zdroj pravdy**, `AgentShop.sln` se z něj **generuje** (`eng/sync-solutions.cs`) |
| CI | GitHub Actions (`ci.yml`) + `copilot-setup-steps.yml` |
| Jazyk | Kód, komentáře, AGENTS.md, instrukce, skills, ADR: **angličtina**. `README.md`: **čeština** |

---

## 2. Nepřekročitelná pravidla

1. **Žádné vymyšlené balíčky ani API.** Používej jen balíčky z tabulky v kapitole 6. Před přidáním **ověř existenci a poslední stabilní verzi** (NuGet MCP server, `dotnet package search <id> --exact-match`, nebo `https://api.nuget.org/v3-flatcontainer/<id-lowercase>/index.json`). Pokud potřebuješ balíček mimo seznam, zastav se a zdůvodni to v reportu – nepřidávej ho sám.
2. **Ověřuj aktuální dokumentaci** (Microsoft Learn MCP, aspire.dev, xunit.net) u všeho, co se mezi verzemi mění: Aspire AppHost SDK, MTP režim `dotnet test`, OpenAPI v .NET 10, `AddNpgsqlDbContext`. Nespoléhej na paměť.
3. **Zakázáno:** MediatR, AutoMapper, FluentAssertions (od v8 komerční), Newtonsoft.Json, Swashbuckle, controllery, `Startup.cs`, generický repository pattern nad EF Core, složky/projekty `Common`/`Helpers`/`Utils`, `async void`, `.Result`/`.Wait()`, `DateTime.Now/UtcNow`, `#pragma warning disable` bez komentáře s důvodem.
4. **Build bez varování.** `TreatWarningsAsErrors` platí od prvního commitu. Varování neobcházej potlačením; když je pravidlo pro daný kontext opravdu nevhodné, potlač ho v `.editorconfig` **s komentářem proč**.
5. **Nic netvrď bez ověření.** Každý příkaz, který zapíšeš do `AGENTS.md`, README nebo skills, musíš skutečně spustit a ověřit. *„An incorrect command repeated with confidence is worse than no command at all."*
6. **Oba solution soubory jsou v kořeni.** Proto **každý** `dotnet build/test/format` příkaz musí explicitně uvádět solution nebo projekt (jinak MSB1011). Primární solution pro příkazy je `AgentShop.slnx`. Projekty se přidávají **jen do `.slnx`**; `.sln` se vždy přegeneruje příkazem `dotnet run eng/sync-solutions.cs`, nikdy se needituje ručně.
7. **Pracuj po fázích (kapitola 9).** Od fáze 1 po každé fázi: build bez varování, testy zelené, `git commit`.

---

## 3. Cílová struktura repozitáře

```
AgentShop/
├─ AGENTS.md                                  # hlavní instrukce pro agenty (EN, stručné, ověřené příkazy)
├─ README.md                                  # česky: co to je, jak spustit, jak číst repo
├─ AgentShop.slnx                             # zdroj pravdy
├─ AgentShop.sln                              # GENEROVANÝ z .slnx (eng/sync-solutions.cs)
├─ global.json
├─ nuget.config                               # nuget.org + package source mapping
├─ Directory.Build.props
├─ Directory.Build.targets
├─ Directory.Packages.props                   # CPM + GlobalPackageReference pro analyzéry
├─ .editorconfig
├─ .gitattributes
├─ .gitignore                                 # dotnet new gitignore
├─ BannedSymbols.txt
├─ .config/dotnet-tools.json                  # dotnet-ef (pinned)
├─ .vscode/mcp.json                           # Microsoft Learn MCP, NuGet MCP (+ Aspire MCP volitelně)
├─ .github/
│  ├─ copilot-instructions.md                 # krátké, odkazuje na AGENTS.md, nic neduplikuje
│  ├─ instructions/
│  │  ├─ csharp.instructions.md               # applyTo: "**/*.cs"
│  │  ├─ tests.instructions.md                # applyTo: "tests/**"
│  │  ├─ ef-migrations.instructions.md        # applyTo: "**/Persistence/**"
│  │  └─ msbuild.instructions.md              # applyTo: "**/*.props,**/*.targets,**/*.csproj,**/*.sln,**/*.slnx"
│  ├─ skills/
│  │  ├─ add-feature-slice/SKILL.md
│  │  ├─ add-ef-migration/SKILL.md
│  │  └─ add-module/SKILL.md
│  ├─ agents/
│  │  └─ architecture-reviewer.agent.md
│  ├─ prompts/
│  │  └─ plan-feature.prompt.md
│  └─ workflows/
│     ├─ ci.yml
│     └─ copilot-setup-steps.yml
├─ eng/
│  ├─ verify.ps1                              # JEDEN příkaz = celá "definice hotovo" (-SkipIntegration bez Dockeru)
│  └─ sync-solutions.cs                       # file-based app: .slnx → .sln; --check pro CI
├─ docs/
│  ├─ architecture.md                         # mapa modulů, Mermaid diagram závislostí, tok událostí
│  └─ adr/
│     ├─ 0001-modular-monolith-with-vertical-slices.md
│     ├─ 0002-no-mediator.md
│     ├─ 0003-result-pattern-and-problem-details.md
│     ├─ 0004-in-process-integration-events.md
│     ├─ 0005-schema-per-module.md
│     ├─ 0006-internals-visible-to-own-tests-only.md
│     └─ 0007-slnx-source-of-truth-sln-generated.md
├─ src/
│  ├─ AgentShop.AppHost/
│  ├─ AgentShop.ServiceDefaults/
│  ├─ AgentShop.Api/                          # host: Program.cs jen skládá moduly
│  ├─ AgentShop.SharedKernel/                 # čistý kernel bez ASP.NET a EF Core
│  ├─ AgentShop.SharedKernel.AspNetCore/      # IEndpoint, Result→IResult, ValidationFilter
│  └─ Modules/
│     ├─ AGENTS.md                            # pravidla pro práci uvnitř modulů ("nearest wins")
│     ├─ Catalog/
│     │  ├─ AgentShop.Modules.Catalog.Contracts/
│     │  └─ AgentShop.Modules.Catalog/
│     ├─ Orders/
│     │  ├─ AgentShop.Modules.Orders.Contracts/
│     │  └─ AgentShop.Modules.Orders/
│     └─ Shipping/
│        ├─ AgentShop.Modules.Shipping.Contracts/
│        └─ AgentShop.Modules.Shipping/
└─ tests/
   ├─ AGENTS.md                               # pravidla pro testy
   ├─ Directory.Build.props                   # importuje kořenový + test-specifická nastavení
   ├─ AgentShop.ArchitectureTests/
   ├─ AgentShop.Modules.Catalog.Tests/        # unit testy domény modulu
   ├─ AgentShop.Modules.Orders.Tests/
   ├─ AgentShop.Modules.Shipping.Tests/
   └─ AgentShop.Api.IntegrationTests/         # WebApplicationFactory + Testcontainers + Verify
```

**Vnitřní struktura modulu** (příklad Orders):

```
AgentShop.Modules.Orders/
├─ OrdersModule.cs                 # JEDINÝ ručně psaný public typ: AddOrdersModule(), MapOrdersModule()
├─ Domain/
│  ├─ Order.cs                     # agregát (privátní settery, factory, invarianty, doménové chyby)
│  ├─ OrderLine.cs
│  ├─ OrderId.cs                   # readonly record struct
│  ├─ OrderStatus.cs
│  └─ OrderErrors.cs               # static class s Error instancemi
├─ Features/
│  ├─ PlaceOrder/PlaceOrder.cs     # ★ ZLATÁ REFERENČNÍ SLICE ★
│  ├─ GetOrder/GetOrder.cs
│  ├─ CancelOrder/CancelOrder.cs
│  └─ MarkOrderShipped/ShipmentDispatchedHandler.cs   # handler integrační události
└─ Persistence/
   ├─ OrdersDbContext.cs           # HasDefaultSchema("orders")
   ├─ OrdersDbContextFactory.cs    # IDesignTimeDbContextFactory pro dotnet ef
   ├─ Configurations/OrderConfiguration.cs
   └─ Migrations/                  # generuje dotnet ef (public partial class) – výjimka z pravidla viditelnosti
```

`*.Contracts` projekty obsahují **jen** veřejné API modulu: rozhraní modulu (`ICatalogModule`), DTO a integrační události. Nesmí záviset na EF Core, ASP.NET Core ani na interních projektech modulů.

**Povolené reference mezi projekty** (vynuť architektonickými testy):

```
Api ──► SharedKernel.AspNetCore, ServiceDefaults, Modules.* (jen kvůli registraci AddXxxModule/MapXxxModule)
Modules.X ──► SharedKernel, SharedKernel.AspNetCore, Modules.X.Contracts, Modules.Y.Contracts
Modules.X.Contracts ──► SharedKernel (nejvýš kvůli IIntegrationEvent)
SharedKernel.AspNetCore ──► SharedKernel
AppHost ──► Api (jako Aspire project resource)
Modules.X ─✗─► Modules.Y (interní projekt jiného modulu) – ZAKÁZÁNO
```

---

## 4. Doména a funkčnost

Rozsah drž malý – cílem je ukázat vzory, ne postavit e-shop. Celkem **~10 slices**.

### Catalog
- Agregát `Product` (`ProductId`, `Sku`, `Name`, `Price` jako `decimal` > 0, `Currency` = `"CZK"`, `IsActive`).
- Slices: `CreateProduct` (POST `/catalog/products`), `GetProduct` (GET `/catalog/products/{id}`), `ListProducts` (GET `/catalog/products?page&pageSize`), `DeactivateProduct` (POST `/catalog/products/{id}/deactivate`).
- `Catalog.Contracts`: `ICatalogModule.GetProductsAsync(IReadOnlyCollection<Guid> productIds, CancellationToken)` → `IReadOnlyList<ProductSnapshot>` (`ProductId`, `Name`, `UnitPrice`, `IsActive`).
- Pravidla: unikátní `Sku` (409 Conflict), deaktivovaný produkt nelze objednat.
- Unikátnost `Sku` vynucuje **unikátní index v DB** s pojmenovaným constraintem (kontrola dotazem předem nestačí – race condition). Porušení (PostgreSQL `23505` s názvem tohoto constraintu) se mapuje na 409.

### Orders
- Agregát `Order` (`OrderId`, `CustomerEmail`, `Lines`, `Status` ∈ {Placed, Cancelled, Shipped}, `PlacedAt`, `Total` počítaný z řádků).
- Slices: `PlaceOrder` (POST `/orders`), `GetOrder` (GET `/orders/{id}`), `CancelOrder` (POST `/orders/{id}/cancel`).
- `PlaceOrder` načte ceny přes `ICatalogModule` (synchronní komunikace), uloží objednávku a **po úspěšném `SaveChangesAsync`** publikuje `OrderPlacedIntegrationEvent`.
- Handler `ShipmentDispatchedIntegrationEvent` → `Order.MarkShipped()`.
- Pravidla: min. 1 řádek, množství 1–100 (FluentValidation); neaktivní/neexistující produkt → `ValidationError` s chybou u pole `lines[i].productId` (400 `ValidationProblem`, stejný tvar jako chyby z FluentValidation); zrušit lze jen `Placed`; `Shipped` nelze zrušit (409).

### Shipping
- Agregát `Shipment` (`ShipmentId`, `OrderId` jako `Guid`, `Status` ∈ {Pending, Dispatched}, `CreatedAt`, `DispatchedAt`).
- Handler `OrderPlacedIntegrationEvent` → vytvoří `Shipment` (Pending). Musí být **idempotentní** (druhé doručení téže události nevytvoří duplicitu): unikátní index na `Shipment.OrderId` s pojmenovaným constraintem; `23505` na tomto constraintu = událost už byla zpracována → úspěch, ne chyba.
- Slices: `GetShipmentByOrder` (GET `/shipping/shipments?orderId=`), `DispatchShipment` (POST `/shipping/shipments/{id}/dispatch`) → publikuje `ShipmentDispatchedIntegrationEvent`.
- Handler `OrderCancelled` **není** v rozsahu – zapiš ho do `docs/architecture.md` jako známé omezení.

### Integrační události (in-process)
- `SharedKernel`: `IIntegrationEvent`, `IIntegrationEventHandler<TEvent>` (`Task HandleAsync(TEvent e, CancellationToken ct)`), `IIntegrationEventPublisher` (`Task PublishAsync<TEvent>(TEvent e, CancellationToken ct) where TEvent : IIntegrationEvent`).
- Implementace `InProcessIntegrationEventPublisher` je **scoped** (aby handlery dostaly `DbContext` téhož requestu): resolve `IEnumerable<IIntegrationEventHandler<TEvent>>` z injektovaného `IServiceProvider` aktuálního scope a zavolá je sekvenčně. Každý handler si sám volá `SaveChangesAsync(ct)` na `DbContext` svého modulu.
- Handlery jsou `internal` a každý modul je v `AddXxxModule()` **explicitně** registruje jako scoped (`services.AddScoped<IIntegrationEventHandler<OrderPlacedIntegrationEvent>, OrderPlacedHandler>()`) – žádné skenování, ať je registrace dohledatelná.
- ADR 0004 musí poctivě popsat trade-off: **žádný outbox** → při pádu mezi commitem a publikací se událost ztratí; budoucí řešení = transactional outbox (např. Wolverine). Tohle je vědomé zjednodušení vzorové aplikace.

---

## 5. Konvence kódu

### 5.1 Tvar slice (závazný vzor)

Každá slice = **jeden soubor** `Features/<Verb><Noun>/<Verb><Noun>.cs` s jednou `internal static class`, ve které jsou vnořené typy. Zlatá referenční slice je `Orders/Features/PlaceOrder/PlaceOrder.cs` – musí být nejčistší ukázkou vzoru a odkazují na ni AGENTS.md i skill `add-feature-slice`.

Orientační tvar (přizpůsob skutečným API, ale zachovej strukturu):

```csharp
namespace AgentShop.Modules.Orders.Features.PlaceOrder;

internal static class PlaceOrder
{
    public sealed record Request(string CustomerEmail, IReadOnlyList<Request.Line> Lines)
    {
        public sealed record Line(Guid ProductId, int Quantity);
    }

    public sealed record Response(Guid OrderId, decimal Total, string Currency);

    internal sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.CustomerEmail).NotEmpty().EmailAddress();
            RuleFor(x => x.Lines).NotEmpty();
            RuleForEach(x => x.Lines).ChildRules(l => l.RuleFor(x => x.Quantity).InclusiveBetween(1, 100));
        }
    }

    internal static async Task<Result<Response>> HandleAsync(
        Request request,
        OrdersDbContext db,
        ICatalogModule catalog,
        IIntegrationEventPublisher publisher,
        TimeProvider clock,
        CancellationToken ct)
    {
        // 1) načti data přes Contracts  2) doménová logika v agregátu  3) SaveChanges  4) publikuj událost
    }

    internal sealed class Endpoint : IEndpoint
    {
        // Route je RELATIVNÍ – MapOrdersModule() mapuje endpointy modulu pod MapGroup("/orders").
        public void MapEndpoint(IEndpointRouteBuilder app) =>
            app.MapPost("/", async (Request request, OrdersDbContext db, ICatalogModule catalog,
                    IIntegrationEventPublisher publisher, TimeProvider clock, CancellationToken ct) =>
                    (await HandleAsync(request, db, catalog, publisher, clock, ct))
                        .ToCreatedResult(r => $"/orders/{r.OrderId}"))
               .AddEndpointFilter<ValidationFilter<Request>>()
               .WithName(nameof(PlaceOrder))
               .WithTags("Orders")
               .Produces<Response>(StatusCodes.Status201Created)
               .ProducesValidationProblem()
               .ProducesProblem(StatusCodes.Status409Conflict);
    }
}
```

Pravidla:
- **Doménová logika patří do agregátu**, ne do handleru. Handler orchestruje (načíst → zavolat metodu agregátu → uložit → publikovat).
- Handlery používají `DbContext` **přímo** (žádné repository). Dotazy pro čtení: `AsNoTracking()` + projekce do `Response`.
- Žádná slice nesmí referencovat typy jiné slice (sdílí se jen `Domain/` a `Persistence/`).
- **Routy:** `MapXxxModule()` vytvoří `MapGroup("/<prefix modulu>")` a zavolá `group.MapEndpoints(typeof(XxxModule).Assembly)`. `MapEndpoints` najde reflexí jen `IEndpoint` typy **z dané assembly** (vč. `internal`), vytvoří instance (bezparametrický konstruktor) a zavolá `MapEndpoint(group)`. Endpointy se neregistrují do globálního DI (jinak by se mapovaly do skupin všech modulů). Routy ve slices jsou relativní (`"/"`, `"/{id:guid}"`, `"/{id:guid}/cancel"`).
- **Validátory:** každý modul registruje validátory `services.AddValidatorsFromAssembly(typeof(XxxModule).Assembly, includeInternalTypes: true)` (bez `includeInternalTypes` se `internal` validátory nenajdou). `ValidationFilter<T>` **vyžaduje** registrovaný `IValidator<T>` – když chybí, vyhodí `InvalidOperationException` se srozumitelnou zprávou (chyba konfigurace se nesmí projevit jako tichý průchod bez validace). Integrační test dokazuje, že nevalidní request vrátí 400 a handler se nezavolá.
- Čas **jen** přes `TimeProvider`; nové ID přes `Guid.CreateVersion7()` (zabaleno v `XxxId.New()`).
- Všechny async metody berou a předávají `CancellationToken`.
- Logování strukturované (`logger.LogInformation("Order {OrderId} placed", id)`), nikdy interpolace.

### 5.2 SharedKernel

- `Result`, `Result<T>`, `Error(string Code, string Description, ErrorType Type)`, `ErrorType` ∈ {Validation, NotFound, Conflict, Failure}. Bez výjimek pro očekávané stavy.
- `ValidationError : Error` (Type = Validation) nese `IReadOnlyDictionary<string, string[]> Errors` (pole → chyby), aby doménové validační chyby z handleru měly **stejný tvar** jako chyby z FluentValidation (`ValidationProblem`).
- `Entity<TId>`, `AggregateRoot<TId>` jen pokud to opravdu zjednoduší kód (žádná spekulativní abstrakce).
- `SharedKernel.AspNetCore`: `IEndpoint`, `EndpointExtensions` (`MapEndpoints(this IEndpointRouteBuilder, Assembly)`), `ResultExtensions` (`ToHttpResult`, `ToCreatedResult` → `TypedResults`; `ValidationError` → `ValidationProblem`, ostatní `ErrorType` → `ProblemDetails` s odpovídajícím status kódem), `ValidationFilter<T>`.

### 5.3 Strongly-typed IDs

```csharp
public readonly record struct OrderId(Guid Value)
{
    public static OrderId New() => new(Guid.CreateVersion7());
}
```

EF Core konverze přes `HasConversion(id => id.Value, value => new OrderId(value))` v konfiguraci entity. Na HTTP a v Contracts se používá `Guid`; převod probíhá v endpointu/handleru.

### 5.4 Viditelnost

- V projektech modulů je **vše `internal`** kromě třídy `XxxModule` (registrace). Veřejné typy jsou jen v `*.Contracts`, `SharedKernel*`, `ServiceDefaults`.
- **Výjimka:** migrace generované `dotnet ef` (`public partial class … : Migration` v `*.Persistence.Migrations`) a jejich model snapshot. Needituj je kvůli viditelnosti; architektonický test je vynechá.
- Vnořené `public` recordy (`Request`, `Response`) uvnitř `internal static class` mají efektivní viditelnost `internal` – to je v pořádku. Pravidlo viditelnosti se proto testuje přes `Assembly.GetExportedTypes()` (typy viditelné mimo assembly), ne přes deklarovaný modifikátor.
- `InternalsVisibleTo` **jen** z modulu na jeho vlastní testovací projekt (`AgentShop.Modules.X` → `AgentShop.Modules.X.Tests`), generované konvencí v `Directory.Build.targets`. ADR 0006 vysvětlí, proč jde o vědomý kompromis (unit testy doménových invariantů), a že nic jiného `InternalsVisibleTo` nepoužívá.

### 5.5 Persistence

- Jedna databáze `agentshop`, schéma per modul (`catalog`, `orders`, `shipping`), vlastní `DbContext` per modul s `HasDefaultSchema("<schéma>")` a vlastní tabulkou historie migrací ve schématu modulu (`npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "<schéma>")`).
- `IDesignTimeDbContextFactory` per modul, aby `dotnet ef migrations add` fungovalo bez spuštění hostu.
- Úvodní migrace `Initial` pro každý modul vygeneruj **nástrojem** `dotnet ef` (lokální tool), ne ručně. Z kořene repozitáře vždy s explicitními argumenty (bez nich `dotnet ef` hledá projekt v aktuální složce a selže):
  ```
  dotnet ef migrations add Initial --project src/Modules/Orders/AgentShop.Modules.Orders --startup-project src/Modules/Orders/AgentShop.Modules.Orders --context OrdersDbContext --output-dir Persistence/Migrations
  ```
  Ověř, že příkaz funguje (design-time factory je v modulu, proto je modul zároveň startup project), a tentýž tvar použij ve skillu `add-ef-migration` a pro `dotnet ef migrations script`.
- Generované migrace jsou pod `TreatWarningsAsErrors` – pokud na nich analyzéry hlásí styl, vyřeš to v `.editorconfig` sekcí `[**/Persistence/Migrations/*.cs]` (`generated_code = true`), ne úpravou vygenerovaného kódu.
- V prostředí `Development` a v testech se migrace aplikují při startu (`MigrateAsync`). Produkční strategii zapiš jako poznámku do ADR 0005.
- Napojení na Aspire: ověř v aktuální dokumentaci správný způsob registrace více `DbContext` nad jedním connection stringem (`AddNpgsqlDbContext` / `EnrichNpgsqlDbContext`) a zvol jej.

### 5.6 Host (`AgentShop.Api/Program.cs`)

Program.cs je krátký a čitelný: `AddServiceDefaults()`, `AddProblemDetails()`, `AddOpenApi()`, `TimeProvider.System`, publisher, `AddCatalogModule()`, `AddOrdersModule()`, `AddShippingModule()`; pak `UseExceptionHandler()`, `MapOpenApi()`, Scalar (jen Development), `MapCatalogModule()`… a `MapDefaultEndpoints()`. Na konci `public partial class Program;` pro `WebApplicationFactory`.

---

## 6. Build infrastruktura

### 6.1 `global.json`

- `sdk.version`: nejnižší dostupná 10.0.x feature band nainstalovaná na stroji, `rollForward: latestFeature`.
- `test.runner`: `"Microsoft.Testing.Platform"` (MTP režim `dotnet test` v .NET 10).
- Pokud to aktuální Aspire podporuje, pinni verzi `Aspire.AppHost.Sdk` v sekci `msbuild-sdks` (jinak ve `Sdk` atributu AppHost projektu).

### 6.2 `Directory.Build.props` (kořen)

Musí obsahovat minimálně: `TargetFramework=net10.0`, `LangVersion=latest`, `Nullable=enable`, `ImplicitUsings=enable`, `TreatWarningsAsErrors=true`, `AnalysisLevel=latest-recommended`, `EnforceCodeStyleInBuild=true`, `GenerateDocumentationFile=true` + `NoWarn` `CS1591` (kvůli IDE0005 v buildu), `Deterministic=true`, `ContinuousIntegrationBuild=true` když `CI=true`, `NuGetAudit=true`, `NuGetAuditMode=all`, a `WarningsNotAsErrors` pro `NU1901;NU1902` (low/moderate zranitelnosti hlásit, high/critical rozbijí build). Každá vlastnost má krátký komentář **proč** (čte to i agent).

Odvozené vlastnosti podle názvu projektu:
- `IsModuleProject` = název začíná `AgentShop.Modules.`, nekončí `.Contracts` ani `.Tests`.
- `IsContractsProject` = název končí `.Contracts`.

### 6.3 `tests/Directory.Build.props`

Importuje kořenový soubor (`$([MSBuild]::GetPathOfFileAbove('Directory.Build.props', '$(MSBuildThisFileDirectory)../'))`), nastaví `IsTestProject=true`, `IsPackable=false`, `OutputType=Exe`, `UseMicrosoftTestingPlatformRunner=true`, přidá společné test `PackageReference` (bez verzí) a `<Using Include="Xunit" />`. V testech povol, co je pro testy nutné (např. veřejné test třídy), a potlač nevhodná pravidla v `tests/.editorconfig` s komentářem.

### 6.4 `Directory.Build.targets`

Jen věci, které patří po vyhodnocení projektu – malý a okomentovaný:
1. Pro `IsModuleProject`: `<InternalsVisibleTo Include="$(MSBuildProjectName).Tests" />`.
2. Pro netestové projekty: `<AdditionalFiles Include="$(MSBuildThisFileDirectory)BannedSymbols.txt" />`.
3. Target `ValidateProjectConventions` (před `Build`), který vyhodí srozumitelnou `<Error>`, když:
   - projekt ve `src/Modules/<Modul>/` nemá název `AgentShop.Modules.<Modul>` nebo `AgentShop.Modules.<Modul>.Contracts`,
   - `*.Contracts` projekt referencuje EF Core nebo `Microsoft.AspNetCore.App`.
   Chybové hlášky piš tak, aby agentovi rovnou řekly, jak chybu opravit.

### 6.5 `Directory.Packages.props` (CPM)

`ManagePackageVersionsCentrally=true`, `CentralPackageTransitivePinningEnabled=true`. Analyzéry jako `GlobalPackageReference`. Balíčky seskupené a okomentované podle účelu. **Poslední stabilní verze ověř těsně před zápisem** – orientační stav k 2026-10-11:

| Balíček | Verze (orientačně) | Účel |
|---|---|---|
| `Microsoft.AspNetCore.OpenApi` | 10.0.x | OpenAPI dokument |
| `Scalar.AspNetCore` | 2.17.x | API UI (Development) |
| `FluentValidation`, `FluentValidation.DependencyInjectionExtensions` | 12.2.x | validace |
| `Npgsql.EntityFrameworkCore.PostgreSQL` | 10.0.x | EF Core provider |
| `Microsoft.EntityFrameworkCore.Design` | 10.0.x | design-time (PrivateAssets=all) |
| `Aspire.Npgsql.EntityFrameworkCore.PostgreSQL` | 13.6.x | Aspire client integrace |
| `Aspire.Hosting.PostgreSQL` | 13.6.x | AppHost |
| ServiceDefaults balíčky (OpenTelemetry, Resilience, ServiceDiscovery) | dle aktuální šablony Aspire | telemetrie, health |
| `xunit.v3` | 4.0.x | testy (MTP v2) |
| `Microsoft.AspNetCore.Mvc.Testing` | 10.0.x | WebApplicationFactory |
| `Testcontainers.PostgreSql` | 4.16.x | reálná DB v testech |
| `Respawn` | 7.0.x | reset DB mezi testy |
| `Verify.XunitV3` | 33.x | snapshot testy |
| `TngTech.ArchUnitNET.xUnitV3` | 0.13.x | architektonické testy |
| `Microsoft.Extensions.TimeProvider.Testing` | 10.x | `FakeTimeProvider` |
| `Microsoft.CodeAnalysis.BannedApiAnalyzers` | 5.x | GlobalPackageReference |
| `Meziantou.Analyzer` | 3.x | GlobalPackageReference |

Ověř kompatibilitu `xunit.v3` 4.x s `Verify.XunitV3` a `TngTech.ArchUnitNET.xUnitV3`. Pokud nejsou kompatibilní, zvol nejvyšší kompatibilní kombinaci a zapiš to do reportu.

### 6.6 `nuget.config`

`<clear />`, jen `nuget.org`, **package source mapping** (`<package pattern="*" />` → nuget.org). Komentář: chrání proti dependency confusion a halucinovaným balíčkům z jiných zdrojů.

### 6.7 `.editorconfig`

- `root = true`, UTF-8, LF (kromě `*.cmd/*.bat` CRLF), 4 mezery pro C#, 2 pro XML/JSON/YAML/props.
- C# styl jako **build warnings** (`EnforceCodeStyleInBuild`): file-scoped namespaces (IDE0161), `using` mimo namespace, IDE0005 (nepoužité usings), `var` preference, expression-bodied members kde je to čitelné, pojmenování (`_camelCase` privátní pole, `PascalCase` konstanty, `I` prefix rozhraní, `Async` suffix u async metod).
- Úpravy závažnosti analyzérů **s komentářem proč**, např. `CA2007` (ConfigureAwait) = none pro ASP.NET Core, `CA1062` = none (řeší nullable), `CA1848` dle uvážení.
- Žádná globální vypnutí celých kategorií.

### 6.8 `BannedSymbols.txt`

Minimálně: `DateTime.Now`, `DateTime.UtcNow`, `DateTimeOffset.Now`, `DateTimeOffset.UtcNow` (→ `TimeProvider`), `Guid.NewGuid()` (→ `Guid.CreateVersion7()` přes `XxxId.New()`), `Task.Result`, `Task.Wait()`, namespace `Newtonsoft.Json`. Každý řádek s textem, co použít místo toho (správný formát doc-comment ID ověř).

### 6.9 Solution soubory

- **`AgentShop.slnx` je jediný zdroj pravdy.** Vytvoř ho `dotnet new sln -n AgentShop --format slnx` a projekty přidávej jen do něj (`dotnet sln AgentShop.slnx add <csproj>`; solution složky odpovídají adresářům). Solution items (`Directory.*.props/targets`, `global.json`, `.editorconfig`, `AGENTS.md`) přidej do `.slnx` ručně jako `<File Path="..." />` ve složce `/Solution Items/`.
- **`AgentShop.sln` se generuje** file-based aplikací `eng/sync-solutions.cs` (`dotnet run eng/sync-solutions.cs`), která čte `.slnx` a zapisuje `.sln` pomocí oficiální knihovny `Microsoft.VisualStudio.SolutionPersistence`. Režim `--check` porovná projekty, jejich solution složky a solution items a při rozdílu skončí exit kódem 1 s hláškou, jak to opravit. `--check` běží v `eng/verify.ps1` i v CI.
- Ověřená kostra (2026-10-11, SDK 10.0.401; knihovna **vyžaduje absolutní cesty**, spouštět z kořene repozitáře; verzi balíčku ověř):

```csharp
#:package Microsoft.VisualStudio.SolutionPersistence@1.0.52
using Microsoft.VisualStudio.SolutionPersistence.Model;
using Microsoft.VisualStudio.SolutionPersistence.Serializer;

var root = Directory.GetCurrentDirectory();
var slnx = Path.Combine(root, "AgentShop.slnx");
var sln = Path.Combine(root, "AgentShop.sln");

var model = await SolutionSerializers.SlnXml.OpenAsync(slnx, CancellationToken.None);
if (args.Contains("--check"))
{
    if (!File.Exists(sln)) { Console.Error.WriteLine("AgentShop.sln missing. Run: dotnet run eng/sync-solutions.cs"); return 1; }
    var existing = await SolutionSerializers.SlnFileV12.OpenAsync(sln, CancellationToken.None);
    if (Signature(existing) != Signature(model)) { Console.Error.WriteLine("AgentShop.sln is out of sync with AgentShop.slnx. Run: dotnet run eng/sync-solutions.cs"); return 1; }
    Console.WriteLine("Solutions in sync."); return 0;
}
await SolutionSerializers.SlnFileV12.SaveAsync(sln, model, CancellationToken.None);
Console.WriteLine("Generated AgentShop.sln from AgentShop.slnx");
return 0;

static string Signature(SolutionModel m) =>
    string.Join('\n', m.SolutionProjects.Select(p => $"{p.Parent?.Path}|{p.FilePath.Replace('\\', '/')}").Order()) + "\n" +
    string.Join('\n', m.SolutionFolders.Select(f => $"{f.Path}:{string.Join(',', (f.Files ?? []).Select(x => x.Replace('\\', '/')).Order())}").Order());
```

- Pozor: `eng/sync-solutions.cs` nesmí být součástí žádného projektu a kořenový `Directory.Build.props` se na něj aplikuje také (`TreatWarningsAsErrors`, analyzéry, `BannedSymbols`). Ověř, že projde; případné výjimky řeš v `eng/Directory.Build.props` s komentářem.

---

## 7. Testy

| Projekt | Obsah | Docker | Trait |
|---|---|---|---|
| `AgentShop.Modules.*.Tests` | Unit testy agregátů a doménových pravidel (FakeTimeProvider), validátorů | ne | – |
| `AgentShop.ArchitectureTests` | ArchUnitNET pravidla (níže) | ne | – |
| `AgentShop.Api.IntegrationTests` | E2E přes HTTP: každá slice min. 1 happy path + 1 chybový stav; tok událostí Orders→Shipping→Orders; souběžné vytvoření stejného `Sku` → 409; dvojí doručení `OrderPlaced` → jeden `Shipment`; Verify snapshoty odpovědí a `/openapi/v1.json` | ano | `Category=Integration` |

**Architektonická pravidla** (každé jako samostatný, srozumitelně pojmenovaný test s `.Because("...")` vysvětlením pro agenta):
1. Interní projekt modulu nezávisí na interních typech jiného modulu (jen na jeho `*.Contracts`).
2. `*.Contracts` nezávisí na EF Core, ASP.NET Core ani interních projektech modulů.
3. `*.Domain` namespace nezávisí na EF Core, ASP.NET Core, `*.Features`, `*.Persistence`.
4. `SharedKernel` nezávisí na žádném modulu ani na ASP.NET Core.
5. Slice nezávisí na jiné slice (`Features.A` ↛ `Features.B`).
6. Z projektu modulu je mimo assembly viditelný (`Assembly.GetExportedTypes()`) jen `XxxModule`; vynechej `*.Persistence.Migrations` (generované EF).
7. Všechny `IEndpoint` implementace leží v `*.Features.*` a jsou `sealed` a ne-public.
8. Integrační události jsou `sealed record` v `*.Contracts` a jejich název končí `IntegrationEvent`.

**Integrační testy:**
- Sdílená fixture startuje **jeden** PostgreSQL kontejner na běh; `WebApplicationFactory<Program>` přepíše connection string.
- **Testy sdílející DB běží sériově** (v integračním projektu vypni paralelizaci, např. `[assembly: CollectionBehavior(DisableTestParallelization = true)]`; ověř aktuální způsob pro xUnit v3) – jinak by reset DB v jednom testu rozbil běžící jiný test.
- **Respawn** před každým testem: `DbAdapter.Postgres`, `SchemasToInclude = ["catalog", "orders", "shipping"]`, `TablesToIgnore` = tabulky `__EFMigrationsHistory` ve všech třech schématech (jinak EF ztratí záznam o aplikovaných migracích), otevřené `NpgsqlConnection` pro `CreateAsync` i `ResetAsync`.
- Testy nesmí záviset na pořadí ani na datech z jiného testu.

**Verify:**
- Odpovědi ověřuj přes `VerifyJson` (nebo serializované objekty), ne jako surový string. Centrální nastavení v `[ModuleInitializer]`: scrubbing GUID a časových údajů (včetně inline hodnot ve stringu, např. v `Location` hlavičce), ignorování proměnlivých hlaviček (porty, `Date`, trace ID). Názvy polí a JSON typy musí v snapshotu zůstat.
- **Snapshot test OpenAPI dokumentu** je klíčový guardrail: každá změna veřejného kontraktu se ukáže jako diff `.verified.json` v PR. `*.received.*` patří do `.gitignore`.
- Stabilitu ověř: po schválení snapshotů spusť testy **podruhé** – musí projít beze změn.

**Příkazy** (ověř je a přesně je zapiš do AGENTS.md; pokud se syntaxe liší, oprav ji podle aktuální dokumentace):
- Rychlé (bez Dockeru): `dotnet test --solution AgentShop.slnx -- --filter-not-trait "Category=Integration"` (pokud projekt po filtru nemá žádné testy a MTP vrátí exit code 8, řeš to `--ignore-exit-code 8` nebo konfigurací v `tests/Directory.Build.props`).
- Úplné (vyžaduje Docker): `dotnet test --solution AgentShop.slnx`.

---

## 8. Kontext pro agenty

### 8.1 `AGENTS.md` (kořen, anglicky, cíl < 120 řádků)

Řiď se principy „Instructions Hygiene" (kapitola 7.2 analýzy) – **jen to, co agent sám nezjistí**:
1. **System** – 3–5 vět: modulární monolit, moduly, kde je zlatá slice, co je generované (migrace).
2. **Build & validate** – ověřené příkazy: restore, build, rychlé testy, úplné testy, format, `dotnet run eng/sync-solutions.cs [--check]`, `pwsh eng/verify.ps1 [-SkipIntegration]`. Upozornění, že kvůli dvěma solution souborům se musí vždy uvádět `AgentShop.slnx` a že `AgentShop.sln` je generovaný.
3. **Definition of done** – `pwsh eng/verify.ps1` je zelené (build bez varování, format, solutions sync, všechny testy; vyžaduje Docker).
4. **Decisions** – výčet lokálních rozhodnutí z kapitoly 1 a 5 (jeden řádek každé).
5. **Hard constraints** – neměnit `.editorconfig`, `Directory.*.props/targets`, `BannedSymbols.txt`, `global.json` ani architektonické testy bez výslovného zadání; nepřidávat balíčky bez zdůvodnění; nikdy neupravovat existující migraci; nikdy nepřijímat Verify snapshot bez kontroly diffu; měnit veřejný JSON kontrakt jen na výslovné zadání.
6. **Where to look** – `docs/architecture.md`, `docs/adr/`, `src/Modules/AGENTS.md`, `tests/AGENTS.md`, skills.

Žádné obecné rady typu „write clean code".

### 8.2 Vnořené `AGENTS.md`
- `src/Modules/AGENTS.md` – tvar slice, viditelnost, komunikace modulů, kde je zlatá slice.
- `tests/AGENTS.md` – který test kam patří, traity, Testcontainers, Verify workflow.

### 8.3 `.github/instructions/*.instructions.md`
Krátké, s `applyTo` frontmatter (viz struktura). `msbuild.instructions.md` hlavně říká: „Neměň bez výslovného zadání; nový projekt přidej jen do `AgentShop.slnx`, pak spusť `dotnet run eng/sync-solutions.cs`. `AgentShop.sln` nikdy needituj ručně."

### 8.4 Skills (`.github/skills/<name>/SKILL.md`)
Frontmatter `name` + **přesný `description`** (podle něj se skill vybírá). Tělo = číslovaný postup s ověřenými příkazy a odkazem na referenční kód:
- `add-feature-slice` – nová slice podle `PlaceOrder`, registrace, unit + integrační test, OpenAPI snapshot.
- `add-ef-migration` – `dotnet ef migrations add <Name> --project <modul> --context <XxxDbContext> --output-dir Persistence/Migrations`, kontrola vygenerovaného SQL (`dotnet ef migrations script`), zákaz destruktivních změn bez zadání.
- `add-module` – projekty Module + Contracts, přidání do `AgentShop.slnx` + `dotnet run eng/sync-solutions.cs`, registrace v Api, schéma, arch testy, testovací projekt, `docs/architecture.md`.

### 8.5 Custom agent `.github/agents/architecture-reviewer.agent.md`
Read-only reviewer (jen čtecí nástroje + spuštění testů). Kontroluje hranice modulů, izolaci slices, viditelnost, zakázané balíčky/API, změny OpenAPI snapshotu, migrace. Výstup: seznam nálezů se závažností a odkazem na pravidlo/ADR.

### 8.6 Prompt `.github/prompts/plan-feature.prompt.md`
Z popisu feature vytvoří plán v `docs/specs/<feature>.md` (dotčené moduly, nové slices, kontrakty, události, migrace, testy, rizika) – **bez psaní kódu**.

### 8.7 `.vscode/mcp.json`
- `microsoft-learn`: HTTP `https://learn.microsoft.com/api/mcp`
- `nuget`: stdio `dnx NuGet.Mcp.Server --source https://api.nuget.org/v3/index.json --yes`
- Aspire: spusť `aspire agent init` (pokud je Aspire CLI k dispozici) a zvol skills; MCP jen volitelně. Pokud CLI není k dispozici, uveď to v reportu a do README napiš, jak to udělat.

### 8.8 `.github/copilot-instructions.md`
Max. 10 řádků: odkaz na `AGENTS.md` + pravidla specifická pro Copilot code review (na co se při review zaměřit). Nic neduplikovat.

---

## 9. Postup práce (fáze)

Od fáze 1 po **každé** fázi: `dotnet build AgentShop.slnx` bez varování → relevantní testy → `git commit` s výstižnou zprávou. Pokud se něco nepovede, oprav příčinu – nepotlačuj varování, nemaž testy.

0. **Průzkum prostředí** (bez buildu a commitu) – `dotnet --list-sdks`, `docker info`, `aspire --version` (volitelné), ověření verzí balíčků. Výsledek stručně shrň.
1. **Kostra** – git, `.gitignore`, `.gitattributes`, `global.json`, `nuget.config`, `Directory.Build.props/targets`, `Directory.Packages.props`, `.editorconfig`, `BannedSymbols.txt`, `AgentShop.slnx`, `eng/sync-solutions.cs` + vygenerovaný `AgentShop.sln`, tool manifest s `dotnet-ef`.
2. **SharedKernel + SharedKernel.AspNetCore** – Result, Error, IEndpoint, ValidationFilter, integrační události + publisher.
3. **Catalog** – modul, Contracts, doména, slices, persistence, migrace, unit testy.
4. **Orders** – včetně zlaté slice `PlaceOrder`, volání Catalog přes Contracts, publikace události, unit testy.
5. **Shipping** – idempotentní handler události, dispatch, zpětná událost do Orders.
6. **Host + Aspire** – Api/Program.cs, ServiceDefaults, AppHost s PostgreSQL, OpenAPI + Scalar.
7. **Testy** – ArchitectureTests, IntegrationTests (Testcontainers, Respawn, Verify, OpenAPI snapshot).
8. **Kontext pro agenty** – AGENTS.md (všechny), instructions, skills, agent, prompt, mcp.json, `aspire agent init`.
9. **CI** – `ci.yml` (setup-dotnet z `global.json`, restore, build Release, format verify, `dotnet run eng/sync-solutions.cs -- --check`, všechny testy včetně integračních na `ubuntu-latest`), `copilot-setup-steps.yml` (job přesně `copilot-setup-steps`, setup-dotnet z `global.json`, `dotnet tool restore`, `dotnet restore AgentShop.slnx`).
10. **Dokumentace** – `docs/architecture.md` (Mermaid diagram modulů a toků událostí, známá omezení), ADR 0001–0007 (krátké: kontext, rozhodnutí, důsledky), `README.md` česky.
11. **Finální ověření** – kapitola 10.

---

## 10. Akceptační kritéria (Definition of Done)

Všechno musí platit a v reportu doložíš výstupem příkazů:

- [ ] `dotnet build AgentShop.sln` **i** `dotnet build AgentShop.slnx` → 0 chyb, **0 varování**.
- [ ] `dotnet format AgentShop.slnx --verify-no-changes` → bez změn.
- [ ] `dotnet run eng/sync-solutions.cs -- --check` → OK; po přidání projektu jen do `.slnx` → selže se srozumitelnou hláškou (ověř a vrať zpět).
- [ ] Rychlé testy (bez Dockeru) zelené; úplné testy (s Dockerem) zelené.
- [ ] `pwsh eng/verify.ps1` → zelené (spouští všechno výše; **vyžaduje Docker**). `eng/verify.ps1 -SkipIntegration` vynechá integrační testy, ale integrační projekt zkompiluje. Pokud Docker není k dispozici, spusť jen `-SkipIntegration`, výslovně to uveď v reportu a **nevydávej to za splněnou Definition of Done**.
- [ ] **Negativní kontrola guardrails** (ověř a pak vrať zpět, necommituj): (a) použití `DateTime.UtcNow` v modulu rozbije build; (b) dočasná `ProjectReference` z Orders na interní projekt Catalog + použití jeho public `CatalogModule` (zkompiluje se) → **selže architektonický test** (ne kompilace); (c) přidání `Version` do `PackageReference` rozbije build; (d) změna response DTO rozbije OpenAPI snapshot test. Výsledek každé kontroly uveď v reportu.
- [ ] Ruční smoke test: aplikace se spustí přes AppHost (pokud je Docker/Aspire k dispozici), `POST /orders` vytvoří objednávku a vznikne `Shipment`.
- [ ] Každý příkaz v AGENTS.md, README a skills byl skutečně spuštěn.
- [ ] Repo neobsahuje zakázané balíčky/API (kapitola 2, bod 3) ani `TODO` bez odkazu na známé omezení.

---

## 11. Závěrečný report

Na konci vypiš stručný report (česky):
1. Co vzniklo (strom nejdůležitějších souborů, počet projektů, slices a testů).
2. Výstupy ověřovacích příkazů (zkráceně: počty testů, varování).
3. Výsledky negativních kontrol guardrails.
4. **Odchylky od zadání** a proč (včetně změněných verzí balíčků).
5. Známá omezení a doporučené další kroky.
