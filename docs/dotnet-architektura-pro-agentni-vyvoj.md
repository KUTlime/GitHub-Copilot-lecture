# Architektura .NET aplikací pro agentní vývoj s LLM

> Hloubková analýza a syntéza veřejně dostupných znalostí (články, oficiální dokumentace, GitHub repozitáře, konferenční videa) – stav k říjnu 2026.
> Cíl: popsat, jak navrhovat a organizovat .NET/C# kód tak, aby s ním AI agenti (GitHub Copilot agent mode / coding agent, Claude Code, Cursor, Codex …) pracovali **spolehlivě, levně (tokeny) a bezpečně**.

---

## Obsah

1. [Shrnutí (TL;DR)](#1-shrnutí-tldr)
2. [Proč na architektuře záleží u agentů ještě víc](#2-proč-na-architektuře-záleží-u-agentů-ještě-víc)
3. [Architektonické styly – srovnání z pohledu agenta](#3-architektonické-styly--srovnání-z-pohledu-agenta)
4. [Vzory na úrovni kódu a jejich vliv na kvalitu generování](#4-vzory-na-úrovni-kódu-a-jejich-vliv-na-kvalitu-generování)
5. [Guardrails: kompilátor, analyzéry, architektonické testy](#5-guardrails-kompilátor-analyzéry-architektonické-testy)
6. [Zpětná vazba: testy jako „definice hotovo“](#6-zpětná-vazba-testy-jako-definice-hotovo)
7. [Kontext pro agenta: AGENTS.md, instrukce, skills](#7-kontext-pro-agenta-agentsmd-instrukce-skills)
8. [MCP servery a runtime kontext (Aspire)](#8-mcp-servery-a-runtime-kontext-aspire)
9. [Pracovní postup: spec → plán → implementace](#9-pracovní-postup-spec--plán--implementace)
10. [Typické chyby LLM v C#/.NET a jejich prevence](#10-typické-chyby-llm-v-cnet-a-jejich-prevence)
11. [Data z praxe: dotnet/runtime a Copilot Coding Agent](#11-data-z-praxe-dotnetruntime-a-copilot-coding-agent)
12. [Referenční architektura „agent-ready .NET“](#12-referenční-architektura-agent-ready-net)
13. [Checklist](#13-checklist)
14. [Otevřené otázky a sporná místa](#14-otevřené-otázky-a-sporná-místa)
15. [Optimalizace na cenu](#15-optimalizace-na-cenu)
16. [Vlastní dojmy](#16-vlastní-dojmy)
17. [Zdroje](#17-zdroje)

---

## 1. Shrnutí (TL;DR)

| # | Princip | Proč to agentovi pomáhá |
|---|---------|-------------------------|
| 1 | **Lokalita změny** – jedna business změna = jedna složka / jeden modul | Méně souborů v kontextu → méně tokenů, menší „context rot“, menší blast radius |
| 2 | **Organizace podle feature (vertical slices), ne podle technické vrstvy** | Skutečná osa je *layered vs. sliced*; Clean/Onion/Hexagonal jsou v .NET prakticky totožné |
| 3 | **Modulární monolit jako výchozí topologie** | Explicitní hranice bez síťových volání a distribuovaných transakcí; mikroservisy agentovi kontext tříští |
| 4 | **Explicitnost v typech** – nullable, strongly-typed IDs, Result/DU, `internal` by default | Chyby agenta se projeví jako *build error*, ne jako tichý bug |
| 5 | **Guardrails jako kód** – `TreatWarningsAsErrors`, analyzéry, `.editorconfig`, architektonické testy | Agent nemůže „tvrdit hotovo“, dokud neprojde build/test |
| 6 | **Rychlá, dvoustupňová testovací smyčka** | Agent iteruje mnohokrát; latence testů se násobí |
| 7 | **Krátké, vysoce signální instrukce** (AGENTS.md) + progresivní odhalování (skills, `*.instructions.md`) | Instrukce jsou rozpočet kontextu; špatný příkaz je horší než žádný |
| 8 | **Grounding přes MCP a runtime data** (Microsoft Learn, NuGet MCP; Aspire CLI/skills) | Omezuje halucinace API/balíčků a dává přístup k reálným logům a trace |
| 9 | **Plan-first workflow a malé PR** | Snižuje „řešení špatného problému“ a usnadňuje review |
| 10 | **Průběžný refaktoring („garbage collection“ kódu)** | Experiment: refaktoring 17k řádkového souboru snížil vstupní tokeny na stejnou změnu o **83 %** |

Hlavní konsenzus napříč zdroji: **„AI-friendly“ design = dobrý design pro lidi, jen s vyššími sázkami.** Thoughtworks to formuluje přímo: *„So far, the best AI-friendly patterns align with established best practices.“*

---

## 2. Proč na architektuře záleží u agentů ještě víc

### 2.1 Kontext je nejvzácnější zdroj

- Anthropic definuje *context engineering* jako hledání „nejmenší možné množiny tokenů s vysokým signálem“ a popisuje **context rot** – schopnost modelu vybavit si informace klesá s rostoucím počtem tokenů.
- Claude Code best practices: *„Claude's context window fills up fast, and performance degrades as it fills… The context window is the most important resource to manage.“*
- Simon Willison / Andrej Karpathy: context engineering je *„umění naplnit kontextové okno přesně tou informací, kterou potřebuje další krok“*.

**Důsledek pro architekturu:** každá vrstva nepřímosti, každý „Common“ projekt a každý soubor navíc, který musí agent otevřít, stojí tokeny i přesnost.

### 2.2 Empirický důkaz: refaktoring šetří tokeny

Giles Edwards-Alexander (martinfowler.com, 30. 7. 2026) – aplikace ~150k LoC napsaná agenty; datová vrstva narostla do **jednoho 17 155řádkového souboru**. Po každém kroku refaktoringu nechal čerstvého sub-agenta provést *identickou* změnu:

| Stav | Největší soubor (LoC) | Vstupní tokeny na změnu |
|------|----------------------:|------------------------:|
| Baseline | 17 155 | 159 564 |
| Po rozdělení na moduly (krok 12) | 9 269 | 104 080 |
| Finální stav (krok 15, 19 souborů) | 3 695 | **27 360 (−83 %)** |

Klíčové zjištění: tokeny zůstávají téměř konstantní, dokud se **nezmenší největší soubor** – pak „spadnou ze skály“. Celkový počet řádků se přitom téměř nezměnil. Velikost a soudržnost souborů je tedy přímo ekonomická veličina.

### 2.3 Agent zrychluje entropii

- **Steve Smith (Ardalis)**, *Why Care About Agent Authored Code Quality*: LLM jsou nedeterministické – na rozdíl od kompilátoru se chyba může objevit při každém volání znovu. *„The Big Ball of Mud architecture is every bit as much a problem for LLMs and agents as it is for human developers.“* Dobrá architektura minimalizuje plochu, která se musí měnit – a to je pro agenty minimálně stejně důležité jako pro lidi.
- **Jimmy Bogard** (autor pojmu Vertical Slice Architecture) nyní VSA přímo prezentuje jako *„Effective Guardrails for AI Development“*: nástroje zrychlují *tvorbu* kódu, ale ne nutně jeho *udržovatelnost* – což se projevuje „pálením tokenů na dlouhých review cyklech“.
- **Thoughtworks Tech Radar** (duben 2025, *Assess*): *AI-friendly code design* – expresivní pojmenování dává doménový kontext, modularita drží kontext AI zvládnutelný, DRY usnadňuje konzistenci. Zároveň varuje před *Complacency with AI-generated code* (Hold, listopad 2025).
- **Birgitta Böckeler** (*Harness Engineering*): udržitelný agentní „harness“ stojí na třech pilířích – **context engineering, architektonická omezení a „garbage collection“ kódu**.
- **Oskar Dudycz** (*Vibing, Harness, and OODA Loops*): LLM svádí přeskočit fáze Observe/Orient a rovnou jednat; udržitelnost dává harness (testy, automatizace, dohledatelnost), ne konkrétní vrstvení.

---

## 3. Architektonické styly – srovnání z pohledu agenta

### 3.1 Skutečná osa: vrstvy vs. řezy

Milan Jovanović (*Clean Architecture vs Onion vs Hexagonal*) ukazuje, že Hexagonal (Cockburn 2005), Onion (Palermo 2008) a Clean (Martin 2012) vedou v .NET k **téměř identické struktuře řešení**. Pro agenta je podstatné jen to, zda je kód organizován **podle technického zájmu** (Controllers/Services/Repositories) nebo **podle funkce** (Features/Orders/CreateOrder).

```
Vrstvená (layered):              Vertical Slice:
📁 Controllers/                  📁 Features/
📁 Services/                       └─📁 Users/
📁 Models/                            └─📁 ExportData/
📁 Repositories/                         ├─ ExportUserData.cs
                                         └─ ExportUserDataEndpoint.cs
```

V layered architektuře i jednoduché „Get Order by ID“ sahá do 4 souborů ve 4 složkách; nová feature znamená zásah do každé vrstvy.

### 3.2 Srovnávací tabulka

| Styl | Lokalita změny | Explicitní hranice | Nároky na kontext | Vhodnost pro agenty | Poznámka |
|------|---------------|--------------------|-------------------|---------------------|----------|
| **Vertical Slice (VSA)** | ★★★★★ | ★★★ (uvnitř modulu) | nízké | **velmi vysoká** | Bogard: „new features only add code, you're not changing shared code“ |
| **Modulární monolit** | ★★★★ | ★★★★★ | nízké–střední | **velmi vysoká** | Ideální obal pro VSA; hranice vynutitelné testy |
| **Clean / Onion / Hexagonal** | ★★ | ★★★★ | střední–vysoké | střední | Dobré hranice, ale změna „protíná“ vrstvy; lze kombinovat s VSA (Ardalis template) |
| **Klasická N-tier** | ★ | ★★ | vysoké | nízká | Rozptýlené změny, tendence k „God services“ |
| **Mikroservisy** | ★★ (napříč repozitáři) | ★★★★★ | velmi vysoké | nízká–střední | Změna přes 5 služeb = horší ekonomika kontextu; žádný zdroj netvrdí, že jsou pro agenty lepší |

### 3.3 Vertical Slice Architecture – doporučená praxe

- **„Files are verbs“** – jeden use case na soubor; soubor se dvěma operacemi jsou „dva slicy, které předstírají, že jsou jeden“ (Jovanović). Agent tak dostane jeden jednoznačný soubor k úpravě.
- **Sdílení ve třech úrovních** (Jovanović): infrastruktura (sdílená) / doména (sdílená – agregáty, value objects) / feature-local. Vyhnout se „šuplíku“ `Common`, který zavádí skrytou vazbu, kterou agent nevidí.
- **Comartin (CodeOpinion)**: VSA *není technická* – jde o soudržnost podle business schopností a je ortogonální k topologii nasazení. *Vertical slices ≠ share nothing* – sdílet agregát je v pořádku, sdílet technickou infrastrukturu riskuje tichou vazbu.
- **Konvence pojmenování** (Anton Martyniuk):

```
Features/
  CreateShipment/
    CreateShipment.Endpoint.cs
    CreateShipment.Handler.cs
    CreateShipment.Mapping.cs
    CreateShipment.Validators.cs
    Events/ShipmentCreatedEvent.cs
```

- Alternativa „vše v jedné statické třídě“ (Comartin, Wolverine skills): `CancelOrder.Command`, `CancelOrder.Handler`, `CancelOrder.Endpoint` jako vnořené typy → nejmenší možná plocha.

```csharp
public static class CancelOrder
{
    public sealed record Command(OrderId OrderId, string Reason);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator() => RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }

    internal static async Task<Result> Handle(Command cmd, OrdersDbContext db, TimeProvider clock, CancellationToken ct)
    {
        var order = await db.Orders.FindAsync([cmd.OrderId], ct);
        if (order is null) return Result.NotFound();
        return order.Cancel(cmd.Reason, clock.GetUtcNow());
    }

    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/orders/{id}/cancel", async (OrderId id, CancelRequest body, OrdersDbContext db, TimeProvider clock, CancellationToken ct) =>
            (await Handle(new Command(id, body.Reason), db, clock, ct)).ToHttpResult());
}
```

### 3.4 Modulární monolit

- Jovanović: *„For most teams, a Modular Monolith is the better choice: clear boundaries and independent modules without network calls, distributed transactions, or the heavy operational overhead of microservices.“*
- **„Change test“** pro hranice modulů: typická business změna by se měla dotknout **právě jednoho modulu** – to je zároveň ideální jednotka práce pro agenta.
- **Izolace dat**: sdílené tabulky → **samostatné schéma (doporučený start)** → samostatná DB → jiná persistence.
- **Komunikace**: synchronně přes veřejné rozhraní modulu (`IOrdersModule`), asynchronně přes integrační události. Nikdy přímý přístup k cizímu `DbContext`.
- **„Start with nothing public. Expose only what other modules actually need.“**
- Comartin (*Modular Monolith Boundaries Done Wrong*): fyzické hranice (projekty, DB) ≠ logické hranice (vlastnictví pravidel).
- Referenční implementace: **kgrzybek/modular-monolith-with-ddd** (modul = Domain/Application/Infrastructure/Tests + vlastní ArchTests).
- Praxe: repozitář *andrekirst/family2* má ADR-001 (12/2025), ve kterém se tým (s Claude Code jako „spolurozhodujícím“) vrátil od 8 mikroslužeb k modulárnímu monolitu.

### 3.5 Clean Architecture – kdy ano a jak

Clean Architecture není „špatně“ – Ardalisův template kombinuje **explicitní vrstvy** (`Core → UseCases → Infrastructure → Web`) s **REPR/feature organizací uvnitř** (FastEndpoints: `Create.cs`, `Create.CreateRequest.cs`, `Create.CreateValidator.cs`) a obsahuje `.github/copilot-instructions.md` se sekcí „Common Gotchas“ psanou přímo proti chybám agentů. Hybrid „Clean + slices“ je legitimní volba pro domény s bohatou logikou.

Jovanović: *Clean Architecture řídí složitost disciplínou vrstev, VSA izolací feature – a vyhrává tam, kde se složitost jednotlivých feature výrazně liší.*

---

## 4. Vzory na úrovni kódu a jejich vliv na kvalitu generování

### 4.1 REPR, Minimal APIs, FastEndpoints

Request–Endpoint–Response sdružuje požadavek, handler a odpověď → méně souborů, které musí agent udržovat konzistentní (vs. controller + service + DTO rozházené po vrstvách).

```csharp
public sealed class CreateUser : Endpoint<CreateUserRequest, CreateUserResponse>
{
    public override void Configure() { Post("/api/users"); AllowAnonymous(); }

    public override async Task HandleAsync(CreateUserRequest req, CancellationToken ct)
        => await Send.OkAsync(new() { FullName = $"{req.FirstName} {req.LastName}" }, ct);
}
```

Doporučení: zvolte **jeden** styl (Minimal APIs *nebo* FastEndpoints *nebo* controllery) a zapište ho do instrukcí – „Add endpoints with Minimal APIs rather than controllers“ je přesně ten typ lokálního rozhodnutí, které model sám nevyvodí (*Instructions Hygiene*, .NET Blog).

Automatické objevování endpointů (marker `IEndpoint` / `IApiEndpoint` + registrace přes reflexi nebo source generator) eliminuje častou chybu agenta – zapomenutou registraci v `Program.cs`.

### 4.2 MediatR, komercializace a alternativy

| Datum | Událost |
|-------|---------|
| 1. 4. 2025 | Bogard oznamuje komercializaci AutoMapper a MediatR |
| 16. 4. 2025 | Dual licence: zdarma < 5 M USD obratu / OSS, jinak placené |
| 2. 7. 2025 | Lucky Penny Software, repozitáře přesunuty, `LicenseKey` |

Alternativy:
- **Wolverine** (JasperFx, MIT) – handlery bez rozhraní, objevované konvencí; méně ceremonie. JasperFx vydal kurátorované **AI skills** pro „Critter Stack“ a nechal Claude převést 10 OSS projektů (MediatR/MassTransit/Clean → Wolverine + VSA) s 107 procházejícími integračními testy.
- **martinothamar/Mediator** – source-generated, AOT-friendly, *„build time errors instead of runtime errors“*.
- **Prosté handlery** (statická metoda / třída injektovaná přímo do endpointu) – nejmenší nepřímost.

**Pomáhá nebo škodí mediator agentovi?** Sporné. Argument proti: řetězec `IRequest → DI → IRequestHandler → pipeline behaviors` je pro agenta (i člověka) obtížně dohledatelný „go to definition“. Wolverine docs výslovně *„very strongly recommend against“* řetězení `Result<T>` přes mediator handlery kvůli „ceremonii a šumu“. Argument pro: jednotný tvar handleru je dobře napodobitelný vzor. **Doporučení:** pokud mediator, pak source-generated (chyby při buildu) a bez řetězení handlerů. Striktně dodržovat konvenci pojmenování handlerů pro snadné nalezení agentem.

### 4.3 Result pattern a diskriminované uniony

- Výjimky pro výjimečné stavy, `Result<T>` pro očekávané doménové chyby – volající vidí možné výsledky **v signatuře**. (Jovanović, Andrew Lock – 4dílná série včetně kritického dílu „Is the result pattern worth it?“).
- Knihovny: **Ardalis.Result** (mapování na HTTP), **FluentResults**, **OneOf** (`OneOf<User, InvalidName, NameTaken>` + vyčerpávající `.Match()`).
- Pro agenta: explicitní návratové typy + exhaustivní `switch` = kompilátor hlídá, že agent ošetřil všechny případy. (Pozn.: tvrzení „DU pomáhají LLM“ v této podobě nikdo přímo neformuloval – jde o odvození z principu „explicit over implicit“.)

### 4.4 Strongly-typed IDs

`GetOrderForUser(Guid orderId, Guid userId)` se zkompiluje i s prohozenými argumenty – přesně ten typ chyby, který agent při generování vícevstupových metod dělá. S distinktními typy je to **build error**.

```csharp
public readonly record struct OrderId(Guid Value);
public readonly record struct UserId(Guid Value);

// nebo source-generátory:
[StronglyTypedId] public partial struct OrderId { }          // andrewlock/StronglyTypedId
[ValueObject<Guid>] public partial struct CustomerId;        // SteveDunn/Vogen (+ analyzér proti nevalidní konstrukci)
```

### 4.5 DDD – bohatý vs. anemický model

Bohatý model (privátní konstruktory, factory metody, invarianty v agregátu) koncentruje pravidla na **jedno místo**, takže agent je nemusí hledat v několika službách a nemůže je snadno obejít. Instrukce typu *„Domain rules belong in `src/Core`, not in controllers or persistence models“* jsou podle .NET týmu přesně ten druh nevyvoditelné informace, který do instrukcí patří.

### 4.6 Viditelnost a minimalismus abstrakcí

Z `CSharpExpert.agent.md` (github/awesome-copilot):
- *„DON'T add interfaces/abstractions unless used for external dependencies or testing.“*
- *„Least-exposure rule: private > internal > protected > public.“*

Agenti mají tendenci nadměrně abstrahovat („interface pro všechno“) – explicitní pravidlo + analyzéry to krotí.

---

## 5. Guardrails: kompilátor, analyzéry, architektonické testy

### 5.1 Kompilátor jako zpětná vazba

`Directory.Build.props` v kořeni repozitáře:

```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <AnalysisLevel>latest-recommended</AnalysisLevel>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <NoWarn>$(NoWarn);CS1591</NoWarn>
    <Deterministic>true</Deterministic>
  </PropertyGroup>
</Project>
```

`Directory.Packages.props` (Central Package Management) – agent nemůže zavést konfliktní verzi; každý nový balíček je vidět v jednom souboru při review:

```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
    <CentralPackageTransitivePinningEnabled>true</CentralPackageTransitivePinningEnabled>
  </PropertyGroup>
  <ItemGroup>
    <PackageVersion Include="FluentValidation" Version="12.0.0" />
    <!-- ... -->
  </ItemGroup>
</Project>
```

`global.json` – pinovaná verze SDK, aby sandbox agenta (např. GitHub Actions u Copilot coding agenta) používal stejný toolchain jako lokální vývoj.

### 5.2 `.editorconfig` + `dotnet format`

- Jeden kořenový `.editorconfig` s `root = true`; závažnost pravidel držet **v `.editorconfig`**, ne v IDE nastavení (managedcode/dotnet-skills).
- CI krok `dotnet format --verify-no-changes`.
- V instrukcích výslovně zakázat agentovi měnit `.editorconfig` a `Directory.Build.props` bez výslovného zadání (praxe z *dailydevops/healthchecks*: *„MUST NEVER change `.editorconfig` unless explicitly asked to“*) – jinak agent „opraví“ build vypnutím pravidla.
- Užitečné analyzéry: vestavěné CA pravidla, `Microsoft.VisualStudio.Threading.Analyzers` (async), `Meziantou.Analyzer`, `Roslynator`, banned API analyzer (`Microsoft.CodeAnalysis.BannedApiAnalyzers` – např. zakázat `DateTime.UtcNow`, `Newtonsoft.Json`).

```text
# BannedSymbols.txt
P:System.DateTime.Now;Use TimeProvider
P:System.DateTime.UtcNow;Use TimeProvider
N:Newtonsoft.Json;Use System.Text.Json
```

### 5.3 Architektonické testy

Kamil Grzybek (ADR 0017): *„it is not possible to enforce the application architecture… using the compiler“* → architektonické testy. Pro agenta jsou to **spustitelná pravidla** místo próz v instrukcích.

**NetArchTest:**

```csharp
[Fact]
public void Domain_ShouldNotDependOn_InfrastructureOrApplication() =>
    Types.InAssembly(DomainAssembly)
        .ShouldNot().HaveDependencyOnAny("MyApp.Application", "MyApp.Infrastructure")
        .GetResult().IsSuccessful.Should().BeTrue();

[Fact]
public void OrdersModule_ShouldNotDependOn_OtherModules() =>
    Types.InAssembly(OrdersAssembly)
        .That().DoNotHaveNameEndingWith("IntegrationEventHandler")
        .Should().NotHaveDependencyOnAny("MyApp.Modules.Billing", "MyApp.Modules.Shipping")
        .GetResult().IsSuccessful.Should().BeTrue();
```

**ArchUnitNET** (TNG, mosty pro xUnit/NUnit/MSTest/TUnit):

```csharp
IArchRule rule = Classes().That().ResideInNamespace("MyApp.Modules.Orders.Internal")
    .Should().BeInternal()
    .Because("modules expose only their public contract");
rule.Check(Architecture);
```

Doporučení (managedcode/dotnet-skills): *„Encode a small number of durable, high-value architecture rules first… Wire architecture tests into the standard test command in AGENTS.md and CI.“* a *„Fail tests on architecture drift, not on temporary style noise.“*

Zajímavé zjištění rešerše: **neexistuje (zatím) široce citovaný článek s tezí „architektonické testy jako guardrail pro AI“** – vzor se šíří zdola, v praxi repozitářů (family2 ADR-012 s „negative test fixtures“, dailydevops/healthchecks, dotnet-skills).

### 5.4 Viditelnost jako hranice

- `internal` pro vše uvnitř modulu, veřejný jen kontrakt modulu.
- Grzybek výslovně **nedoporučuje** `InternalsVisibleTo` kvůli unit testům domény – testovat přes veřejné API. Tým ASP.NET Core v instrukcích pro agenty zakazuje nové `InternalsVisibleTo` i `[UnsafeAccessor]`.
- V praxi to nebude tak horký problém, pokud je vzorec v kódu zavedený.

---

## 6. Zpětná vazba: testy jako „definice hotovo“

> „Give Claude a check it can run“ – testy, build nebo hook, který nedovolí ukončit práci, dokud kontroly neprojdou. (Claude Code best practices)

### 6.1 Dvoustupňová smyčka

| Úroveň | Příkaz | Kdy |
|--------|--------|-----|
| Rychlá | `dotnet test --filter "Category!=Integration"` | po každém kroku agenta |
| Úplná | `dotnet test` (Testcontainers, `WebApplicationFactory`) | před dokončením / v CI |
| Styl | `dotnet format --verify-no-changes` | před dokončením |
| Architektura | `dotnet test tests/ArchitectureTests` | součást rychlé úrovně (je rychlá) |

### 6.2 Nástroje

- **xUnit v3 + Microsoft.Testing.Platform** (stack dotnet/aspire) – strukturovanější výstup snáze parsovatelný nástroji.
- **TUnit** – source-generated, paralelní ve výchozím stavu, rychlé; dodává vlastní `skills/tunit/SKILL.md`.
- **Verify** (snapshot testy) – má `docs/ai-usage.md` s hotovým SKILL.md; výjimky jsou **záměrně strojově parsovatelné**, takže agent umí rozhodnout, zda snapshot přijmout. Pozor: snapshoty agent nesmí přijímat bez lidské kontroly sémantiky.
- **WebApplicationFactory** – in-memory test server pro testy API (doporučený test „na úrovni slice“).
- **Testcontainers for .NET** – reálné Postgres/SQL Server/Redis místo mocků DB.

### 6.3 Varování z praxe

Data z dotnet/runtime: **65,7 % řádků přidaných agentem byly testy** (vs. 49,9 % u lidí) a *„CCA can produce tests that validate existing incorrect behavior“*. Testy psané agentem tedy reviewujte stejně pečlivě jako produkční kód. TDD s agentem (Anthropic): nejprve testy z očekávaných vstupů/výstupů → ověřit, že selhávají → commit → implementace **bez úprav testů**.

### 6.4 Ochrana před nevalidním push

- Můžeme použít nástroje typu Git hook pro přidaní třetí smyčky.
- Dobré kontrolovat s pomocí MS Build přítomnost hooku.
- Nutné přizpůsobit práci ve worktrees.

```msbuild
<Target Name="CheckPrePushHook" BeforeTargets="Build" Condition="'$(CI)' == '' AND '$(TF_BUILD)' == '' AND '$(GITHUB_ACTIONS)' == ''">
    <!-- Ask git for the hook location so that worktrees (shared common dir) and core.hooksPath are honored. -->
    <Exec Command="git rev-parse --git-path hooks/pre-push"
          WorkingDirectory="$(MSBuildThisFileDirectory)"
          ConsoleToMSBuild="true"
          IgnoreExitCode="true"
          IgnoreStandardErrorWarningFormat="true"
          StandardOutputImportance="low"
          StandardErrorImportance="low"
          EchoOff="true">
      <Output TaskParameter="ConsoleOutput" PropertyName="_PrePushHookGitOutput" />
      <Output TaskParameter="ExitCode" PropertyName="_PrePushHookGitExitCode" />
    </Exec>
    <Message Condition="'$(_PrePushHookGitExitCode)' != '0'" Importance="high" Text="Skipping pre-push git hook check: unable to resolve the hook path with git." />
    <PropertyGroup Condition="'$(_PrePushHookGitExitCode)' == '0'">
      <PrePushHookPath>$([System.IO.Path]::GetFullPath($([System.IO.Path]::Combine('$(MSBuildThisFileDirectory)', '$(_PrePushHookGitOutput.Trim())'))))</PrePushHookPath>
    </PropertyGroup>
    <Error Condition="'$(_PrePushHookGitExitCode)' == '0' AND !Exists('$(PrePushHookPath)')" Text="Pre-push git hook not found at: $(PrePushHookPath). Please ensure the pre-push hook is installed (worktrees share the hooks of the main repository)." />
  </Target>
```

## 7. Kontext pro agenta: AGENTS.md, instrukce, skills

### 7.1 Formáty

| Formát | Rozsah | Poznámka |
|--------|--------|----------|
| `AGENTS.md` | libovolná složka – **nejbližší vyhrává** | Otevřený standard (agents.md), > 60k projektů; čte ho Copilot, Codex, Cursor, Claude Code |
| `.github/copilot-instructions.md` | celé repo | GitHub Copilot |
| `.github/instructions/*.instructions.md` | glob přes `applyTo` | např. `applyTo: "src/**/*.cs"` |
| `*.prompt.md` | ručně (`/název`) | opakované úlohy |
| `*.agent.md` | persona + povolené nástroje + model | např. „architecture reviewer“ |
| `SKILL.md` (Agent Skills) | načte se podle `description` | **progresivní odhalování** – metadata vždy, tělo jen při použití |
| `CLAUDE.md` | project / local / user | Claude Code (umí číst i AGENTS.md) |

### 7.2 Co do instrukcí patří (podle .NET týmu – *Instructions Hygiene*, 8/2026)

Cíl není nejkratší soubor, ale **nejmenší množina vysoce signálních informací, které spolehlivě mění výsledek**. Otázka zní: *„Co model potřebuje vědět, co sám nedokáže spolehlivě zjistit, odvodit ani dohledat?“*

1. **Nezřejmá fakta o systému** – které hranice jsou záměrné, co je generované, co „vypadá staře“, ale běží v produkci.
2. **Nejkratší spolehlivá cesta k validaci** – *jen ověřené příkazy*. *„An incorrect command repeated with confidence is worse than no command at all.“*
3. **Rozhodnutí, která codebase sama konzistentně neurčuje** – MSTest vs. xUnit, Minimal APIs vs. controllery, `Result<T>` vs. výjimky, `TimeProvider`.
4. **Tvrdá omezení a drahé chyby** – JSON kontrakt, logování osobních dat, zpětně kompatibilní migrace.
5. **Kde je zdroj pravdy** – odkazy na `docs/`, ADR, `global.json` (progresivní odhalování).

**Co tam nepatří:** obecné rady („piš čistý kód“, „dodržuj best practices“) – model je zná a jen spotřebují rozpočet. Silná slova (*always/never/must*) rezervovat pro skutečně absolutní pravidla.

### 7.3 Vzor `AGENTS.md` pro .NET aplikaci

```markdown
# AGENTS.md

## Systém
Modulární monolit, ASP.NET Core (.NET 10), EF Core, PostgreSQL. Řešení: `App.slnx`.
Moduly: `src/Modules/{Orders,Billing,Shipping}` – každý modul = vertical slices v `Features/`.
- Moduly spolu komunikují JEN přes `*.Contracts` projekt nebo integrační události.
- `src/Clients/Generated` se generuje (`./eng/gen-clients.ps1`) – nikdy needitovat ručně.

## Build a validace (ověřené příkazy)
- `dotnet build App.slnx`
- Rychlé testy: `dotnet test App.slnx --filter "Category!=Integration"`
- Úplné testy (vyžaduje Docker): `dotnet test App.slnx`
- Formát: `dotnet format App.slnx --verify-no-changes`
Hotovo = build bez warningů + rychlé testy + architektonické testy zelené.

## Rozhodnutí
- Nová feature = nová složka `Features/<Verb><Noun>/` (endpoint + handler + validator v jednom souboru).
- Minimal APIs, ne controllery. FluentValidation. System.Text.Json (nikdy Newtonsoft).
- Očekávané doménové chyby → `Result<T>`; výjimky jen pro neočekávané stavy.
- Čas přes `TimeProvider`, ID jako strongly-typed `record struct`.
- Vše `internal`, pokud to jiný modul nepotřebuje.
- Testy: xUnit v3, integrační přes `WebApplicationFactory` + Testcontainers.

## Tvrdá omezení
- NEMĚŇ `.editorconfig`, `Directory.Build.props`, `Directory.Packages.props` ani architektonické testy bez výslovného zadání.
- Nepřidávej NuGet balíčky bez zmínky v PR popisu; verze jen v `Directory.Packages.props`.
- Migrace EF Core: jedna logická změna = jedna migrace; nikdy neupravuj už vydanou migraci.
- Neloguj osobní údaje.

## Kde hledat
- Architektonická rozhodnutí: `docs/adr/`
- API konvence: `docs/api-guidelines.md`
- Specifičtější pravidla: `AGENTS.md` v podsložkách (nejbližší vyhrává).
```

### 7.4 Jak to dělá Microsoft

- **dotnet/runtime**: repo-wide `copilot-instructions.md` + **23** oblastních `*.instructions.md` s `applyTo` (JIT, C#, testy…). Pravidla: *„Don't claim more than you verified“*, *„One concern per PR“*, regresní test ke každému bug fixu, *„Delete flaky and low-value tests rather than patching them.“* Odkazuje na skills `build-and-test` a `code-review`.
- **dotnet/aspnetcore**: repo-wide instrukce + **vnořené `AGENTS.md`** (`src/Mvc`, `src/Components`, `eng/common`); minimální diff (*„Preserve untouched code exactly as written“*).
- **dotnet/aspire**: kořenový `AGENTS.md`, 12 `*.instructions.md`, vlastní `.agent.md` (např. reviewer architektury).
- **dotnet/maui**: SDK z `global.json`, explicitní varování před zastaralými názvy pipeline.
- **Ardalis** (*Optimizing AI Agents with Progressive Disclosure*, 4/2026): „skills-as-selectors“ – kořenový soubor krátký, detaily ve skills načítaných na vyžádání.

### 7.5 Hotové knihovny instrukcí a skills

- **github/awesome-copilot** – `csharp.instructions.md`, `dotnet-architecture-good-practices.instructions.md`, `CSharpExpert.agent.md`, skills `csharp-xunit`, `csharp-tunit`, `csharp-async`, `nuget-manager`…
- **managedcode/dotnet-skills** – skills pro ArchUnitNET, NetArchTest, analyzéry, modulární monolit; governance pravidlo, aby agent *nikdy nezeslaboval* existující pravidla v AGENTS.md.
- **microsoft/aspire-skills**, **JasperFx Critter Stack skills**, **Verify** a **TUnit** SKILL.md – trend: knihovny dodávají instrukce pro agenty spolu s kódem.

---

## 8. MCP servery a runtime kontext (Aspire)

### 8.1 .NET Aspire jako „agent-ready“ platforma

#### Jaký problém Aspire řeší

Agent standardně vidí **jen zdrojový kód**. Typická .NET aplikace je ale za běhu víc než kód – API, background worker, PostgreSQL, Redis, fronta, frontend. Když nahlásíte „POST /orders vrací 500“, agent bez další pomoci:

- neví, co běží, na jakých portech a s jakými connection stringy,
- nevidí logy ani cestu requestu napříč službami,
- **hádá z kódu**, případně mu logy ručně kopírujete do chatu.

Dokumentace Aspire to shrnuje: *„Without Aspire: Agents only see fragments of your setup and guess at runtime state.“* Aspire se proto v letech 2025–2026 explicitně přepozicoval jako *„Agent-ready, code-first.“*

#### Co je Aspire (stručně)

- **AppHost** – malý C# projekt, který popisuje celou aplikaci: služby, závislosti a pořadí startu.
- **`aspire start` / `aspire run`** – vše spustí lokálně (včetně kontejnerů) a automaticky propojí connection stringy a service discovery.
- **ServiceDefaults** – sdílený projekt, který do každé služby přidá OpenTelemetry (logy, trace, metriky) a health checks.
- **Dashboard** – stav služeb, konzolové a strukturované logy, distribuované trace. Od Aspire 13.6 s persistencí, výslovně proto, že „dashboard sdílí stroj s editorem, agenty, buildy…“.

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var db    = builder.AddPostgres("postgres").AddDatabase("ordersdb");
var cache = builder.AddRedis("cache");

builder.AddProject<Projects.Api>("api")
    .WithReference(db).WithReference(cache)
    .WaitFor(db);

builder.AddProject<Projects.Worker>("worker").WithReference(db);

builder.Build().Run();
```

#### Co z toho má agent

| # | Schopnost | Bez Aspire | S Aspire |
|---|-----------|------------|----------|
| 1 | **Mapa aplikace** | Skládá dohromady `docker-compose`, `appsettings`, README | Celá topologie v jednom C# souboru (AppHost) |
| 2 | **Sám aplikaci spustí a ověří** | Spouštíte mu ji vy, nebo se zasekne na interaktivním výstupu | `aspire start` (na pozadí, neinteraktivně), `aspire wait` (počká, až je vše healthy) |
| 3 | **Vidí, co se za běhu skutečně děje** | Hádá z kódu nebo čte, co mu vložíte | `aspire describe`, `aspire logs`, `aspire otel logs`, `aspire otel traces` – vše s `--format Json` |

Díky tomu zvládne agent celou ladicí smyčku sám:

> spustit (`aspire start` + `aspire wait`) → zopakovat chybu → `aspire describe` (co je unhealthy) → `aspire otel logs` / `aspire otel traces` (příčina) → opravit kód → zopakovat request → **ověřit nový trace**

Agent tak **opravuje podle skutečných dat, ne podle odhadu**. Dokumentace přitom upozorňuje: *zelené health checks samy o sobě nedokazují, že je chybný request opravený* – je potřeba request zopakovat.

#### Skills + CLI jako hlavní cesta, MCP jako volitelný doplněk

- **Hlavní cesta: `aspire agent init`** (nabízí se i při `aspire new` / `aspire init`). Detekuje použité AI prostředí (Copilot, Claude Code, Cursor, Codex, OpenCode…) a nainstaluje **skills** (např. `.agents/skills/aspire/SKILL.md`, `.agents/skills/aspire-orchestration/SKILL.md`), které agenta naučí pracovat s Aspire CLI. CLI samo o sobě umí runtime data číst – MCP k tomu není potřeba.
- **Volitelně: Aspire MCP server** (`aspire agent init --mcp`, případně výběr v interaktivním průvodci). Zpřístupní stejná data jako MCP nástroje: `list_resources`, `list_console_logs`, `list_structured_logs`, `list_traces`, `list_trace_structured_logs`, `execute_resource_command`, `search_docs`, `doctor`… Vhodné, pokud váš agent pracuje lépe s MCP než s terminálem.
- **VS Code extension** navíc nabízí nástroje `#aspireStartAppHost` / `#aspireStopAppHost` (s potvrzením uživatele).
- Pokud máte `AGENTS.md` vygenerovaný starší verzí Aspire, **nahraďte ho skills** – tak to doporučuje oficiální dokumentace.

#### Doporučení

**Kdy to dává smysl:**
- ✅ Aplikace s více procesy/závislostmi (API + worker + DB + cache/fronta + frontend) – největší přínos.
- ➖ Jednoduché API s jednou databází – menší přínos, ale telemetrie a dashboard pomohou i tak.

**Postup:**
1. **Přidejte AppHost + ServiceDefaults** (`aspire init` do existujícího řešení) a namodelujte v AppHostu **všechny** služby a závislosti. Neudržujte souběžně ruční `docker-compose` – vznikly by dva zdroje pravdy.
2. **ServiceDefaults ve všech službách**, aby každá posílala logy a trace.
3. **Strukturované logování:** `logger.LogInformation("Order {OrderId} created", id)`, ne `$"Order {id} created"` – jen strukturované logy jde filtrovat a propojit s trace.
4. **Health checks** – aby `aspire wait` mělo podle čeho poznat připravenost.
5. **Spusťte `aspire agent init`** – skills; MCP jen pokud ho chcete.
6. **Doplňte do `AGENTS.md`:**
   ```markdown
   - Aplikaci spouštěj přes `aspire start` + `aspire wait`, ne `dotnet run` jednotlivých projektů.
   - U chyb za běhu nejdřív zjisti příčinu z logů a trace (`aspire describe`, `aspire otel logs`, `aspire otel traces`), teprve pak měň kód.
   - Po opravě zopakuj request a ověř nový trace – zelené health checks nestačí.
   ```
7. **Bezpečnost:** MCP server běží lokálně jako podproces (STDIO, bez otevřených síťových portů). Prostředky s citlivými daty vyřaďte přes `.ExcludeFromMcp()`.
8. **Paralelní agenti** ve více git worktree: `aspire start --isolated`, aby nekolidovaly porty a user secrets.

**Jednou větou:** Aspire dává agentovi spustitelnou mapu aplikace a přístup k jejím logům a trace – agent tak umí chybu sám zopakovat, najít příčinu a ověřit opravu, místo aby hádal z kódu.

### 8.2 Další MCP servery pro .NET

| Server | K čemu |
|--------|--------|
| **Microsoft Learn MCP** (`https://learn.microsoft.com/api/mcp`, zdarma, bez auth) | Aktuální oficiální dokumentace → proti zastaralým/vymyšleným API |
| **NuGet MCP** (např. DimonSmart/NugetMcpServer) | Reálná metadata balíčků → proti halucinovaným balíčkům |
| **MSBuild binlog MCP** (`Microsoft.AITools.BinlogMcp`, dotnet/skills) | Strukturované chyby buildu místo parsování logů |
| **Roslyn-based MCP** (komunitní) | Sémantická navigace/refaktoring (find references, rename) místo grep |
| **Playwright MCP** | E2E ověření UI, kombinovatelné s Aspire |
| **C# MCP SDK** (`ModelContextProtocol`) | Vlastní MCP server pro doménové nástroje |

### 8.3 `dotnet run app.cs`

.NET 10 umožňuje spouštět jednosouborové aplikace bez `.csproj` (`#:package`, `#:sdk Microsoft.NET.Sdk.Web`), s „povýšením“ přes `dotnet project convert`. Pro agenta to zkracuje smyčku *prototypuj → ověř* (spike, ověření chování knihovny) bez zásahu do řešení.

### 8.4 Prostředí cloudového agenta

`.github/workflows/copilot-setup-steps.yml` (job musí být pojmenován `copilot-setup-steps`, max. 59 min) – předinstaluje SDK z `global.json`, provede restore, případně runtime pro MCP servery. dotnet/aspnetcore v něm výslovně instaluje .NET 8 a 10 „pro MCP servery“.

---

## 9. Pracovní postup: spec → plán → implementace

1. **Explore → Plan → Implement → Commit** (Anthropic). *„If you could describe the diff in one sentence, skip the plan.“*
2. **Spec-driven development – GitHub Spec Kit**: `/speckit-constitution` → `/speckit-specify` → `/speckit-plan` → `/speckit-tasks` → `/speckit-implement`. *„Intent is the source of truth.“* Existují .NET dema – greenfield CLI i brownfield ASP.NET CMS (~307k LoC), kde agent nejprve vygeneroval „constitution“ analýzou existujícího kódu.
3. **Mapování na .NET architekturu:** jeden task = jeden slice / jeden endpoint / jedna migrace. Po každém tasku `dotnet build` + rychlé testy.
4. **Malé PR, jedna starost na PR** (dotnet/runtime). Review se přesouvá ke specifikacím a akceptačním kritériím – Faros AI data (via Latent Space): týmy s vysokou adopcí AI mergují o 98 % víc PR, ale doba review roste o 91 %.
5. **Ukotvení k referenční aplikaci** (Böckeler, *Anchoring AI to a reference application*): „zlatá cesta“ – vzorová feature/modul, na který instrukce odkazují („nový slice dělej jako `Features/Orders/CreateOrder`“). Agent kopíruje vzory, které vidí; nejlepší instrukce je dobrý příklad v kódu.
6. **Pravidelný refaktoring** jako investice do budoucí ceny změn (viz §2.2).

---

## 10. Typické chyby LLM v C#/.NET a jejich prevence

| Chyba | Projev | Prevence |
|-------|--------|----------|
| **Zastaralá API** | `Startup.cs`, `WebHost`, `System.Data.SqlClient`, staré EF API | Instrukce („minimal hosting, žádný Startup.cs“), Microsoft Learn MCP, `LangVersion`/`TargetFramework` v props |
| **Halucinované NuGet balíčky („slopsquatting“)** | Studie: 19,7 % doporučených balíčků neexistuje, 43 % halucinací se opakuje (zneužitelné) | NuGet MCP, CPM (jedno místo pro review), package source mapping, lock files, zákaz přidávat balíčky bez zmínky |
| **Newtonsoft vs. System.Text.Json** | Míchání serializerů, rozbitý kontrakt | Výslovné pravidlo + BannedApiAnalyzer |
| **Async chyby** | `async void`, `.Result`/`.Wait()`, chybějící `CancellationToken` | VS Threading analyzéry, CA2007/CA2016, `TreatWarningsAsErrors` |
| **EF Core migrace** | Destruktivní `RenameColumn`/drop; v modulárních architekturách design-time model „nevidí“ celý model | Review SQL diffu, jedna změna = jedna migrace, CI gate na drop/rename, idempotentní skripty |
| **Nadměrná abstrakce** | Interface ke každé třídě, zbytečné vrstvy | Pravidlo „no interface unless external dependency/testing“, VSA |
| **Obcházení guardrails** | Vypnutí pravidla v `.editorconfig`, `#pragma warning disable`, `[Skip]` u testu | Zákaz v instrukcích, CODEOWNERS na konfigurační soubory, analyzér/CI kontrola pragma |
| **Testy potvrzující chybné chování** | Agent „opraví“ test místo kódu | TDD s commitem testů předem, review testů |
| **Neověřená tvrzení** | „Hotovo, testy prochází“ bez spuštění | *„Don't claim more than you verified“*, stop hook / CI jako zdroj pravdy |
| **`DateTime.Now`, statický stav** | Netestovatelný kód | `TimeProvider`, banned symbols |

---

## 11. Data z praxe: dotnet/runtime a Copilot Coding Agent

Stephen Toub, *Ten Months with Copilot Coding Agent in dotnet/runtime* (.NET Blog, 3/2026), období 5/2025–3/2026:

| Kategorie | PR | Úspěšnost (merge) |
|-----------|---:|------------------:|
| Lidé (Microsoft) | 3 082 | 87,1 % |
| Lidé (komunita) | 1 411 | 79,7 % |
| **Copilot Coding Agent** | **878** (535 merged, +95k/−31k řádků) | **67,9 %** |
| Boti | 810 | 85,9 % |

- **Kvalita instrukcí měřitelně rozhoduje:** bez `copilot-instructions.md` úspěšnost **38,1 %**; po přidání build instrukcí a konfigurace firewallu výrazně vyšší.
- Revert rate: 0,6 % (agent) vs. 0,8 % (ostatní).
- Podle typu úlohy: úklid/odstranění 84,7 %, testy 75,6 %, bug fix 69,4 %, feature 64,5 %, **výkon 54,5 %** (agent neumí spolehlivě posoudit, zda změna výkon zlepšuje).
- Srovnání repozitářů: roslyn 74,7 %, aspnetcore 71,2 %, efcore 69,2 %, aspire 64,8 %, MCP csharp-sdk 77,3 %.
- Vývoj instrukcí: vytvoření → firewall/cesta k dotnet → rozdělení na oblastní soubory → **odstranění povinného plného buildu** (nechat agenta rozhodnout) → přidání code-review skillu.

**Interpretace pro architekturu:** agent je nejsilnější na dobře ohraničených, mechanicky ověřitelných úlohách. Čím víc úloh v codebase má tento tvar (malý slice, jasný test, rychlý build), tím vyšší podíl práce lze delegovat.

---

## 12. Referenční architektura „agent-ready .NET“

```
/
├─ AGENTS.md                          # krátký, vysoce signální, ověřené příkazy
├─ .github/
│  ├─ copilot-instructions.md         # (volitelně – nebo jen AGENTS.md)
│  ├─ instructions/
│  │  ├─ csharp.instructions.md       # applyTo: "**/*.cs"
│  │  ├─ tests.instructions.md        # applyTo: "tests/**"
│  │  └─ migrations.instructions.md   # applyTo: "**/Migrations/**"
│  ├─ skills/                         # např. new-feature-slice, ef-migration, investigate-with-aspire
│  ├─ agents/architecture-reviewer.agent.md
│  └─ workflows/copilot-setup-steps.yml
├─ global.json
├─ Directory.Build.props              # Nullable, TreatWarningsAsErrors, AnalysisLevel, EnforceCodeStyleInBuild
├─ Directory.Packages.props           # Central Package Management
├─ .editorconfig                      # root = true, závažnosti pravidel
├─ BannedSymbols.txt
├─ docs/
│  ├─ adr/                            # architektonická rozhodnutí (agent je čte i zapisuje)
│  └─ specs/                          # Spec Kit / specifikace
├─ src/
│  ├─ AppHost/                        # .NET Aspire – model celé aplikace
│  ├─ ServiceDefaults/                # OpenTelemetry, health checks
│  ├─ Api/                            # host: Program.cs skládá moduly
│  ├─ Shared/Kernel/                  # jen skutečně sdílené: Result, strongly-typed ID base, domain events
│  └─ Modules/
│     ├─ Orders/
│     │  ├─ Orders.Contracts/         # JEDINÉ veřejné API modulu (DTO, integrační události, IOrdersModule)
│     │  └─ Orders/                   # vše internal
│     │     ├─ AGENTS.md              # specifika modulu (volitelně)
│     │     ├─ Domain/                # agregáty, value objects, invarianty
│     │     ├─ Features/
│     │     │  ├─ CreateOrder/CreateOrder.cs     # request + validator + handler + endpoint
│     │     │  ├─ CancelOrder/CancelOrder.cs
│     │     │  └─ GetOrder/GetOrder.cs
│     │     ├─ Persistence/           # OrdersDbContext (vlastní schéma), konfigurace, migrace
│     │     └─ OrdersModule.cs        # AddOrdersModule() / MapOrdersEndpoints()
│     └─ Billing/ ...
└─ tests/
   ├─ ArchitectureTests/              # NetArchTest / ArchUnitNET – hranice modulů, vrstvy, viditelnost
   ├─ Modules.Orders.Tests/           # testy slices přes WebApplicationFactory + Testcontainers
   └─ ...
```

**Proč tato kombinace:**
- *Modulární monolit* dává tvrdé hranice (projekty, `internal`, schéma DB) → agent nemůže omylem „sáhnout vedle“, a když to zkusí, selže build nebo arch test.
- *Vertical slices uvnitř modulu* dávají lokalitu změny → typická úloha = 1 nová složka + 1 testovací soubor.
- *Domain/* drží invarianty na jednom místě → slices jsou tenké a agent je napodobí.
- *Aspire* dává agentovi runtime pohled (logy, trace) přes Aspire CLI + skills, volitelně přes MCP.
- *Guardrails jako kód* + *ověřené příkazy v AGENTS.md* = jasná „definice hotovo“.

---

## 13. Checklist

**Struktura**
- [ ] Feature-first organizace (slices); žádné globální složky `Services/`, `Repositories/`.
- [ ] Modulární monolit s `*.Contracts` projekty a `internal` by default.
- [ ] Žádný „šuplík“ `Common` – sdílený jen kernel s jasným účelem.
- [ ] Soubory malé a soudržné; žádné „God files“ (sledovat největší soubory).
- [ ] Referenční („zlatá“) feature, na kterou instrukce odkazují.

**Typy a explicitnost**
- [ ] `<Nullable>enable</Nullable>`, `TreatWarningsAsErrors`, `AnalysisLevel`, `EnforceCodeStyleInBuild`.
- [ ] Strongly-typed IDs, `Result<T>` pro očekávané chyby, `TimeProvider`.
- [ ] Jeden zvolený styl endpointů, jeden serializer, jeden testovací framework.

**Guardrails**
- [ ] Central Package Management + `global.json`.
- [ ] `.editorconfig` + `dotnet format --verify-no-changes` v CI.
- [ ] Architektonické testy (hranice modulů, závislosti vrstev, viditelnost).
- [ ] BannedApiAnalyzer pro zakázaná API.
- [ ] CODEOWNERS / zákaz v instrukcích pro konfigurační soubory a arch testy.

**Zpětná vazba**
- [ ] Rychlá testovací úroveň (< 1–2 min) s filtrem; úplná úroveň v CI.
- [ ] Integrační testy slices přes `WebApplicationFactory` + Testcontainers.
- [ ] Snapshot testy (Verify) pro kontrakty API.

**Kontext**
- [ ] `AGENTS.md`: systém, ověřené příkazy, lokální rozhodnutí, tvrdá omezení, odkazy.
- [ ] Oblastní `*.instructions.md` s `applyTo`; skills pro opakované postupy.
- [ ] ADR v `docs/adr/`.
- [ ] Pravidelná „hygiena instrukcí“ – mazat zastaralá a obecná pravidla.

**Nástroje**
- [ ] Microsoft Learn MCP, NuGet MCP.
- [ ] U distribuovaných aplikací Aspire: `aspire agent init` (skills), volitelně Aspire MCP; v AGENTS.md pravidlo „nejdřív logy/trace, pak kód“.
- [ ] `copilot-setup-steps.yml` pro cloudového agenta.

**Proces**
- [ ] Plan-first pro vícesouborové změny; spec-driven pro větší feature.
- [ ] Jedna starost na PR, malé diffy, review testů i snapshotů.
- [ ] Pravidelný refaktoring jako investice do ceny budoucích změn.

---

## 14. Otevřené otázky a sporná místa

- **Mediator ano/ne:** Jednotný tvar handlerů vs. nepřímost. Trend: source-generated mediator nebo konvenční handlery (Wolverine) nebo přímé volání.
- **DRY vs. duplicita ve slices:** VSA toleruje duplicitu mezi slices; Thoughtworks zdůrazňuje DRY pro konzistenci AI. Kompromis: sdílet doménu (agregáty, value objects), duplikovat „lepidlo“ (mapování, DTO).
- **Délka instrukcí:** Silnější modely potřebují méně procedurálních rad; dotnet/runtime dokonce odstranil povinný plný build. Instrukce je třeba průběžně *odebírat*, nejen přidávat.
- **Review při vysoké propustnosti agentů:** Přesun od review diffů k review specifikací a akceptačních kritérií (Latent Space, StrongDM) – zatím bez konsenzu.
- **Diskurz je mladý (2025–2026):** většina explicitně „AI-framed“ obsahu vznikla po polovině roku 2025; mnoho vazeb je doloženo praxí repozitářů, ne systematickým výzkumem. Jediný nalezený kvantitativní experiment o vlivu struktury kódu na tokeny je Fowlerův článek (Rust, ne .NET) a data Microsoftu z dotnet/runtime.

---

## 15. Optimalizace na cenu

* Chceme maximalizovat využití spálených tokenů => minimalizace context rot a nevalidních změn.
* Nemá smysl bojovat s agenty, levnější je se přizpůsobit (_např. UTF8 bez BOM než trvat na BOM_)
* Generové init nemá moc smysl a je potřeba ho velice zvážit.
* Delegování práce na kompilátor (např.  _Roslyn Source Code generators_)

---

## 16. Vlastní dojmy

* Palme tokeny dokud je to ještě levné.
* Disciplína, disciplína a ještě jednou disciplína. Musí být stanovena nějaká štábní kultura a jet se podlé ní.
* Člověk je úzké hrdlo a největší brzda.
* Optimalizace na cenu nedává zatím moc smysl. Časem se to asi srovná samo s cenou a vývojem technologie.
* TDD (Test-Driven Development) je důležitější než kdy dříve při práci s agenty.
* Pravidelná revize instrukcí a guardrails je klíčová pro udržení kvality kódu při práci s agenty.
* Udržování dobré dokumentace a komentářů je nezbytné pro orientaci agentů v kódu je důležitější než kdy dříve.
* Držet dokumentaci mimo kód (např. v samostatných souborech nebo wiki nebo Jira) nedává moc smysl.
* Integrovaná prostředí typu Atlasian časem asi zvítězí.
* Azure DevOps je mrtvý a obtížně se integruje s moderními agentními workflow.

---

## 17. Zdroje

Legenda: 🎥 = video · ✅ = obsah ověřen přímým načtením při tvorbě tohoto dokumentu (ostatní zdroje ověřovali výzkumní sub-agenti).

### Architektura a principy
- ✅ Jimmy Bogard – *Vertical Slice Architecture Webinar: Effective Guardrails for AI Development* (2026) – https://www.jimmybogard.com/vertical-slice-architecture-webinar/
- Jimmy Bogard – *Vertical Slice Architecture* – https://www.jimmybogard.com/vertical-slice-architecture/
- ✅ Steve Smith (Ardalis) – *Why Care About Agent Authored Code Quality* (2026) – https://ardalis.com/why-care-about-agent-authored-code-quality/
- Ardalis – *Optimizing AI Agents with Progressive Disclosure* (2026-04-17) – https://ardalis.com/optimizing-ai-agents-with-progressive-disclosure/
- Ardalis – *AI Benefits – But at What Cost?* (2026-03-19) – https://ardalis.com/ai-benefits---but-at-what-cost/
- Ardalis – CleanArchitecture template + copilot-instructions – https://github.com/ardalis/CleanArchitecture
- Milan Jovanović – *Vertical Slice vs Clean Architecture* – https://milanjovanovic.tech/blog/vertical-slice-vs-clean-architecture
- Milan Jovanović – *Clean Architecture vs Onion vs Hexagonal* – https://milanjovanovic.tech/blog/clean-architecture-vs-onion-vs-hexagonal
- Milan Jovanović – *When to Choose Vertical Slice Architecture* – https://milanjovanovic.tech/blog/when-to-choose-vertical-slice-architecture
- Milan Jovanović – *Vertical Slice Project Structure in .NET* – https://milanjovanovic.tech/blog/vertical-slice-project-structure-dotnet
- Milan Jovanović – *VSA: Where Does the Shared Logic Live?* – https://milanjovanovic.tech/blog/vertical-slice-architecture-where-does-the-shared-logic-live
- Milan Jovanović – *Modular Monolith vs Microservices* – https://milanjovanovic.tech/blog/modular-monolith-vs-microservices
- Milan Jovanović – *How to Define Module Boundaries* – https://milanjovanovic.tech/blog/module-boundaries-bounded-contexts
- Milan Jovanović – *Modular Monolith Data Isolation* – https://milanjovanovic.tech/blog/modular-monolith-data-isolation
- Milan Jovanović – *Modular Monolith Communication Patterns* – https://milanjovanovic.tech/blog/modular-monolith-communication-patterns
- Milan Jovanović – *Internal vs. Public APIs in Modular Monoliths* – https://milanjovanovic.tech/blog/internal-vs-public-apis-in-modular-monoliths
- Milan Jovanović – *Should You Split That Into Microservices?* (2026-08-01) – https://milanjovanovic.tech/blog/should-you-split-that-into-microservices-ask-these-5-questions-first
- Derek Comartin – *Vertical Slice Architecture Isn't Technical* – https://codeopinion.com/vertical-slice-architecture-isnt-technical/ · 🎥 https://www.youtube.com/watch?v=MWHZBWhITUo
- Derek Comartin – *Vertical Slices Doesn't Mean "Share Nothing"* – https://codeopinion.com/vertical-slices-doesnt-mean-share-nothing/ · 🎥 https://youtu.be/ZiZoR4cQhGE
- Derek Comartin – *Modular Monolith Boundaries Done Wrong* – https://codeopinion.com/modular-monolith-boundaries/ · 🎥 *You're Building Modular Monoliths All Wrong* https://youtu.be/I4dC_lqS_I8
- 🎥 Derek Comartin – *Organizing Commands, Events & Handlers* – https://www.youtube.com/watch?v=8j5ETvSSNpc
- Anton Martyniuk – VSA články – https://antondevtips.com/
- Kamil Grzybek – *Modular Monolith: A Primer* – https://kamilgrzybek.com/blog/posts/modular-monolith-primer
- Kamil Grzybek – *modular-monolith-with-ddd* (ADR 0017 architektonické testy) – https://github.com/kgrzybek/modular-monolith-with-ddd
- Oskar Dudycz – *Vibing, Harness, and OODA Loops* (2026) – https://event-driven.io/en/vibing_harness_and_ooda_loops/
- Oskar Dudycz – *We All Wanted to Be Netflix…* – https://event-driven.io/en/we-all-wanted-to-be-netflix-who-do/
- andrekirst/family2 (ADR-001, ADR-012) – https://github.com/andrekirst/family2

### Obecné principy AI-friendly kódu a context engineering
- ✅ Thoughtworks Technology Radar – *AI-friendly code design* (2025-04-02) – https://www.thoughtworks.com/radar/techniques/ai-friendly-code-design
- ✅ Giles Edwards-Alexander – *The Economic Benefit of Refactoring* (martinfowler.com, 2026-07-30) – https://martinfowler.com/articles/exploring-gen-ai/refactoring-economic-benefit.html
- Birgitta Böckeler – *Context Engineering for Coding Agents* (2026-02-05) – https://martinfowler.com/articles/exploring-gen-ai/context-engineering-coding-agents.html
- Birgitta Böckeler – *Anchoring AI to a Reference Application* (2025-09-25) – https://martinfowler.com/articles/exploring-gen-ai/anchoring-to-reference.html
- Birgitta Böckeler – *Harness Engineering – first thoughts* (2026-02-17) – https://martinfowler.com/articles/exploring-gen-ai/harness-engineering-memo.html
- Anthropic – *Effective context engineering for AI agents* – https://www.anthropic.com/engineering/effective-context-engineering-for-ai-agents
- Anthropic – *Best practices for Claude Code* – https://code.claude.com/docs/en/best-practices
- Simon Willison – *Context engineering* (2025-06-27) – https://simonwillison.net/2025/Jun/27/context-engineering/
- LangChain – *Context Engineering for Agents* – https://blog.langchain.com/context-engineering-for-agents/ · 🎥 https://youtu.be/4GiqzUHD5AA
- humanlayer – *12-factor agents* – https://github.com/humanlayer/12-factor-agents
- Latent Space – *Code Reviews Are Dead* (2026) – https://www.latent.space/p/reviews-dead
- Simon Willison – *How StrongDM's AI team build serious software…* (2026-02-07) – https://simonwillison.net/2026/Feb/7/software-factory/

### Vzory na úrovni kódu
- FastEndpoints – https://github.com/FastEndpoints/FastEndpoints · REPR: https://deviq.com/design-patterns/repr-design-pattern
- Jimmy Bogard – *AutoMapper and MediatR Going Commercial* (2025-04-01) – https://www.jimmybogard.com/automapper-and-mediatr-going-commercial/
- Jimmy Bogard – *Licensing Update* (2025-04-16) – https://www.jimmybogard.com/automapper-and-mediatr-licensing-update/
- Jimmy Bogard – *Commercial Editions Launch* (2025-07-02) – https://www.jimmybogard.com/automapper-and-mediatr-commercial-editions-launch-today/
- Jeremy Miller – *JasperFx's Plans for Marten & Wolverine* (2025-04-02) – https://jeremydmiller.com/2025/04/02/a-quick-note-about-jasperfxs-plans-for-marten-wolverine/
- Jeremy Miller – *Critter Stack Sample Projects and Our Curated AI Skills* (2026-04-15) – https://jeremydmiller.com/2026/04/15/critter-stack-sample-projects-and-our-curated-ai-skills/
- Wolverine – *Wolverine for MediatR Users* – https://wolverinefx.io/introduction/from-mediatr.html
- Wolverine – *Railway Programming* – https://wolverinefx.io/tutorials/railway-programming.html
- martinothamar/Mediator – https://github.com/martinothamar/Mediator
- 🎥 Nick Chapsas – *Using MediatR in .NET? Maybe replace it with this* – https://www.youtube.com/watch?v=aaFLtcf8cO4
- Milan Jovanović – *Functional Error Handling with the Result Pattern* – https://milanjovanovic.tech/blog/functional-error-handling-in-dotnet-with-the-result-pattern
- Andrew Lock – *Working with the Result Pattern* (série) – https://andrewlock.net/working-with-the-result-pattern-part-1-replacing-exceptions-as-control-flow/
- Andrew Lock – *Strongly-typed IDs* (série) – https://andrewlock.net/using-strongly-typed-entity-ids-to-avoid-primitive-obsession-part-1/
- Milan Jovanović – *Strongly Typed IDs in C#* – https://milanjovanovic.tech/blog/strongly-typed-ids-csharp
- Milan Jovanović – *Rich vs. Anemic Domain Model* – https://milanjovanovic.tech/blog/rich-vs-anemic-domain-model
- Knihovny: Ardalis.Result https://github.com/ardalis/Result · FluentResults https://github.com/altmann/FluentResults · OneOf https://github.com/mcintyre321/OneOf · StronglyTypedId https://github.com/andrewlock/StronglyTypedId · Vogen https://github.com/SteveDunn/Vogen

### Guardrails a testy
- Milan Jovanović – *Enforcing Software Architecture With Architecture Tests* – https://milanjovanovic.tech/blog/enforcing-software-architecture-with-architecture-tests
- NetArchTest – https://github.com/BenMorris/NetArchTest
- ArchUnitNET – https://github.com/TNG/ArchUnitNET
- dailydevops/healthchecks (ArchUnitNET + AGENTS.md) – https://github.com/dailydevops/healthchecks
- managedcode/dotnet-skills – https://github.com/managedcode/dotnet-skills
- Microsoft – *Central Package Management* – https://learn.microsoft.com/en-us/nuget/consume-packages/central-package-management
- Verify – *AI usage* (SKILL.md) – https://github.com/VerifyTests/Verify/blob/main/docs/ai-usage.md
- TUnit (skills/tunit/SKILL.md) – https://github.com/thomhurst/TUnit

### Instrukce pro agenty a data z praxe
- ✅ Stephen Toub – *Ten Months with Copilot Coding Agent in dotnet/runtime* (2026-03-23) – https://devblogs.microsoft.com/dotnet/ten-months-with-cca-in-dotnet-runtime/
- ✅ .NET Blog – *Instructions Hygiene – What Frontier Models Still Need You to Say* (2026-08-12) – https://devblogs.microsoft.com/dotnet/instructions-hygiene-what-frontier-models-still-need-you-to-say/
- AGENTS.md – https://agents.md/
- GitHub Docs – *Adding repository custom instructions* – https://docs.github.com/en/copilot/how-tos/copilot-on-github/customize-copilot/add-custom-instructions/add-repository-instructions
- GitHub Docs – *Customize the agent environment (copilot-setup-steps.yml)* – https://docs.github.com/en/copilot/how-tos/copilot-on-github/customize-copilot/customize-cloud-agent/customize-the-agent-environment
- GitHub Docs – *Research, plan, and iterate with Copilot cloud agent* – https://docs.github.com/en/copilot/how-tos/copilot-on-github/use-copilot-agents/research-plan-iterate
- GitHub Blog – *5 tips for writing better custom instructions for Copilot* – https://github.blog/ai-and-ml/github-copilot/5-tips-for-writing-better-custom-instructions-for-copilot/
- VS Code Docs – custom instructions / custom agents / agent skills – https://code.visualstudio.com/docs/agent-customization/custom-instructions · https://code.visualstudio.com/docs/agent-customization/custom-agents · https://code.visualstudio.com/docs/agent-customization/agent-skills
- Anthropic – *Agent Skills* – https://platform.claude.com/docs/en/agents-and-tools/agent-skills/overview
- Claude Code – *Memory (CLAUDE.md)* – https://code.claude.com/docs/en/memory
- github/awesome-copilot – https://github.com/github/awesome-copilot
- dotnet/runtime – https://github.com/dotnet/runtime/blob/main/.github/copilot-instructions.md
- dotnet/aspnetcore – https://github.com/dotnet/aspnetcore
- dotnet/aspire AGENTS.md – https://github.com/dotnet/aspire/blob/main/AGENTS.md
- dotnet/maui – https://github.com/dotnet/maui

### Aspire a MCP
- ✅ aspire.dev – *Use AI coding agents* (ověřeno 2026-10-11) – https://aspire.dev/get-started/ai-coding-agents/
- ✅ aspire.dev – *Debug application failures with AI coding agents* – https://aspire.dev/dashboard/ai-coding-agents/
- ✅ aspire.dev – *Aspire MCP server* – https://aspire.dev/get-started/aspire-mcp-server/
- aspire.dev – *Aspire skills* – https://aspire.dev/get-started/aspire-skills/
- Microsoft – *Aspire 13* – https://devblogs.microsoft.com/aspire/aspire13/
- James Newton-King – *Adding persistence to the Aspire dashboard* (2026-10-08) – https://devblogs.microsoft.com/aspire/aspire-dashboard-persistence/
- Tommaso Stocchi – *Distributed multi-agent systems with Aspire and Microsoft Agent Framework* (2026-06-09) – https://devblogs.microsoft.com/aspire/building-distributed-multi-agent-systems-with-aspire-and-microsoft-agent-framework/
- Adam Ratzman – *Aspire in VS Code: the 13.4 developer loop* (2026-06-16) – https://devblogs.microsoft.com/aspire/aspire-vscode-extension-13-4/
- microsoft/aspire-skills – https://github.com/microsoft/aspire-skills
- Microsoft Learn MCP Server – https://github.com/MicrosoftDocs/mcp
- C# MCP SDK – https://github.com/modelcontextprotocol/csharp-sdk
- .NET Blog – *Build a Model Context Protocol (MCP) Server in C#* – https://devblogs.microsoft.com/dotnet/build-a-model-context-protocol-mcp-server-in-csharp/
- DimonSmart/NugetMcpServer – https://github.com/DimonSmart/NugetMcpServer
- .NET Blog – *Introducing `dotnet run app.cs`* – https://devblogs.microsoft.com/dotnet/announcing-dotnet-run-app/

### Spec-driven development
- GitHub Spec Kit – https://github.com/github/spec-kit
- Den Delimarsky – *Spec-driven development with AI* (2025-09-02) – https://github.blog/ai-and-ml/generative-ai/spec-driven-development-with-ai-get-started-with-a-new-open-source-toolkit/
- .NET dema: https://github.com/mnriem/spec-kit-dotnet-cli-demo · https://github.com/mnriem/spec-kit-aspnet-brownfield-demo

### Rizika LLM
- Microsoft Learn – *GitHub Copilot upgrade* – https://learn.microsoft.com/en-us/dotnet/core/porting/github-copilot-upgrade/overview
- Spracklen et al. – *We Have a Package for You!* (arXiv:2406.10279) – https://arxiv.org/abs/2406.10279
- Socket.dev – *The Rise of Slopsquatting* (2025) – https://socket.dev/blog/slopsquatting-how-ai-hallucinations-are-fueling-a-new-class-of-supply-chain-attacks
- Ivan Rossouw – *When the Tool Cannot See the Whole Model* (EF Core) – https://dev.to/iqtechsolutions/when-the-tool-cannot-see-the-whole-model-1p9p
- Milan Jovanović – *EF Core Migrations Best Practices* – https://milanjovanovic.tech/blog/ef-core-migrations-best-practices

### Videa (konference)
- 🎥 Microsoft Build 2026 – *Using AI tools to teach old apps new tricks (BRK220)* – https://www.youtube.com/watch?v=VyKWfAuAEbY
- 🎥 .NET Conf 2025 – *Understanding Agentic Development* – https://www.youtube.com/watch?v=V_sIYwc9Tvs
- 🎥 .NET Conf 2025 – *Model Context Protocol (MCP) for .NET Developers* – https://www.youtube.com/watch?v=3swFQjBZDUA
- 🎥 .NET Conf 2025 – *Building Remote MCP Servers* – https://www.youtube.com/watch?v=b9cwTqr1cKo
- 🎥 .NET Conf 2025 – *Aspire: Cloud-Native Development Simplified* – https://www.youtube.com/watch?v=aXouOsBh4ro
- 🎥 .NET Conf 2025 – *Deep Dive: Extending and Customizing Aspire* – https://www.youtube.com/watch?v=UQiL3nbQbtM
- 🎥 .NET Conf 2025 – *Building Intelligent Apps with .NET* – https://www.youtube.com/watch?v=N0DzWMkEnzk
- 🎥 Milan Jovanović – *One Engineer, $120K in Token Spend, 800K Lines of Rust* – https://youtu.be/dN11EApmfkM

> **Metodická poznámka:** Rešerše proběhla přes oficiální dokumentaci, blogy, RSS/sitemapy a GitHub. Obsah YouTube a blogy bez dobré indexace (např. Nick Chapsas) jsou pokryty slaběji; u autorů Jason Taylor, Kamil Grzybek a Nick Chapsas nebyl nalezen specifický obsah o architektuře pro AI agenty.
