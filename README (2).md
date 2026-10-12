# AgentShop – vzorová „agent-ready" .NET aplikace

Vzorový repozitář ke školení **GitHub Copilot & .NET – architektura a vývoj aplikací s podporou AI**.
Ukazuje, jak navrhnout ASP.NET Core Web API tak, aby s ním AI agenti (GitHub Copilot, Claude Code, Codex…)
pracovali **spolehlivě, levně a bezpečně**: malé soudržné soubory, explicitní hranice a guardraily,
které chyby agenta promění v chybu buildu nebo testu.

Principy vycházejí z analýzy *Architektura .NET aplikací pro agentní vývoj s LLM* (repozitář školení, `docs/`).

## Co tu najdete

| Oblast | Řešení |
|---|---|
| Architektura | Modulární monolit (`Catalog`, `Orders`, `Shipping`) + vertical slices uvnitř modulů |
| API | ASP.NET Core Minimal APIs, .NET 10, OpenAPI 3.1 + Scalar |
| Doména | Agregáty s invarianty, strongly-typed IDs, Result pattern, ProblemDetails |
| Data | EF Core + PostgreSQL, schéma a `DbContext` pro každý modul |
| Komunikace modulů | `*.Contracts` (rozhraní + integrační události), in-process publisher |
| Orchestrace | .NET Aspire 13.6 (AppHost + ServiceDefaults, OpenTelemetry, dashboard) |
| Guardraily | `TreatWarningsAsErrors`, analyzéry, `BannedSymbols.txt`, CPM, package source mapping, konvence v `Directory.Build.targets` |
| Testy | xUnit v3 na Microsoft.Testing.Platform, ArchUnitNET, Testcontainers, Respawn, snapshot testy OpenAPI kontraktu |
| Kontext pro agenty | `AGENTS.md` (kořen + vnořené), `.github/instructions`, skills, vlastní review agent, MCP servery |

## Rychlý start

Požadavky: .NET 10 SDK (verze viz `global.json`), Docker, PowerShell 7 (`pwsh`).

```powershell
dotnet tool restore                                   # dotnet-ef
dotnet build AgentShop.slnx                           # 0 varování, jinak build selže
dotnet test --solution AgentShop.slnx -- --filter-not-trait "Category=Integration"   # rychlé testy bez Dockeru
dotnet test --solution AgentShop.slnx                 # všechny testy (Docker)
pwsh eng/verify.ps1                                   # kompletní „definice hotovo" = totéž, co CI
dotnet run --project src/AgentShop.AppHost            # aplikace + PostgreSQL + Aspire dashboard
```

API běží na `http://localhost:5080` (Scalar UI: `/scalar/v1`, OpenAPI: `/openapi/v1.json`),
Aspire dashboard na `http://localhost:15000`.

> V kořeni jsou dva solution soubory. Bez explicitní cesty skončí `dotnet build` chybou MSB1011,
> proto všechny příkazy uvádějí `AgentShop.slnx`. `AgentShop.sln` se generuje (`dotnet run eng/sync-solutions.cs`).

## Jak číst repozitář

1. [`AGENTS.md`](AGENTS.md) – co agent potřebuje vědět a sám nezjistí (příkazy, rozhodnutí, tvrdá omezení).
2. [`docs/architecture.md`](docs/architecture.md) – mapa modulů, závislosti, tok událostí, známá omezení.
3. Zlatá referenční slice: [`PlaceOrder.cs`](src/Modules/Orders/AgentShop.Modules.Orders/Features/PlaceOrder/PlaceOrder.cs).
4. [`docs/adr/`](docs/adr) – proč je to postavené právě takhle (8 krátkých ADR).
5. Guardraily: [`Directory.Build.props`](Directory.Build.props), [`Directory.Build.targets`](Directory.Build.targets),
   [`.editorconfig`](.editorconfig), [`BannedSymbols.txt`](BannedSymbols.txt),
   [`tests/AgentShop.ArchitectureTests`](tests/AgentShop.ArchitectureTests).

## Práce s agenty

- **Instrukce:** `AGENTS.md` (kořen, `src/Modules/`, `tests/`), `.github/copilot-instructions.md` (zaměření code review),
  `.github/instructions/*.instructions.md` (podle cesty souborů).
- **Skills** (`.github/skills/`): `add-feature-slice`, `add-ef-migration`, `add-module` + oficiální Aspire skills
  (`aspire`, `aspire-orchestration`, `aspire-monitoring`, …) a `dotnet-inspect` (ověřování API a balíčků místo hádání).
- **Vlastní agent:** `.github/agents/architecture-reviewer.agent.md` – read-only review hranic modulů a guardrailů.
- **Prompt:** `/plan-feature` – vytvoří specifikaci v `docs/specs/` bez psaní kódu.
- **MCP** (`.vscode/mcp.json`): Microsoft Learn (aktuální dokumentace) a NuGet (skutečná metadata balíčků).
- **Copilot coding agent:** prostředí připraví `.github/workflows/copilot-setup-steps.yml`.

### Aspire skills a MCP

Aspire skills byly vygenerovány příkazem (Aspire CLI není potřeba instalovat globálně, spouští se přes `dnx`):

```powershell
$env:ASPIRE_CLI_TELEMETRY_OPTOUT='true'
dnx Aspire.Cli@13.6.1 --yes -- agent init --workspace-root . --skill-locations github `
  --skills aspire,aspire-deployment,aspire-init,aspire-monitoring,aspire-orchestration,aspire-project-v2-migration,aspireify,dotnet-inspect `
  --mcp=false --non-interactive
```

> **Pozor:** `aspire agent init` (13.6) mimo repozitář nainstaloval telemetrický hook pro GitHub Copilot CLI
> (`~/.copilot/hooks/aspire-telemetry.json`) a se skillem `playwright-cli` i globální npm balíček `@playwright/cli`.
> Telemetrii vypnete `ASPIRE_CLI_TELEMETRY_OPTOUT=true`; po spuštění zkontrolujte `~/.copilot/hooks`.
> Volitelný Aspire MCP server přidáte stejným příkazem s `--mcp`.

## Snapshot testy

Integrační testy porovnávají JSON odpovědi a celý OpenAPI dokument se schválenými soubory v
`tests/AgentShop.Api.IntegrationTests/Snapshots/`. Změna API kontraktu je tak vidět jako diff v pull requestu.
Záměrnou změnu přijmete takto (nejdřív zkontrolujte `*.received.json`!):

```powershell
$env:AGENTSHOP_ACCEPT_SNAPSHOTS='1'; dotnet test --project tests/AgentShop.Api.IntegrationTests; Remove-Item Env:AGENTSHOP_ACCEPT_SNAPSHOTS
```

Proč ne Verify: [ADR 0008](docs/adr/0008-snapshot-testing-without-verify.md).

## Ukázka API

```http
POST /catalog/products            { "sku": "KB-001", "name": "Keyboard", "price": 1299.90 }
POST /orders                      { "customerEmail": "jan@example.com", "lines": [ { "productId": "<id>", "quantity": 2 } ] }
GET  /shipping/shipments?orderId=<orderId>
POST /shipping/shipments/<id>/dispatch
GET  /orders/<orderId>            -> "status": "Shipped"
```
