# Obsah kurzu: Vývoj .NET aplikací s podporou GitHub Copilot

---

# Úvod do GitHub Copilota

## Co je GitHub Copilot
- AI nástroj od GitHubu postavený na velkých jazykových modelech (LLM).
- Pomáhá psát kód rychleji, s menším úsilím a s vyšší kvalitou.
- Dostupný jako součást GitHub předplatného – Free, Pro, Business, Enterprise.
- Výzkumy ukazují až 55% nárůst produktivity vývojářů.

## Kde ho lze využít
- IDE: VS Code, Visual Studio, JetBrains, Xcode, Eclipse.
- Terminál: GitHub Copilot CLI.
- Web: GitHub.com – chat, code review, issues, pull requesty.
- Mobilní aplikace: GitHub Mobile jako chatové rozhraní.

## Přehled funkcí – asistivní
- **Inline completion** – automatické návrhy kódu přímo při psaní.
- **Copilot Chat** – konverzační rozhraní pro dotazy k projektu a kódu.
- **Code review** – AI generované návrhy při revizi pull requestu.
- **PR summary** – automatický popis změn v pull requestu.

## Přehled funkcí – agentní
- **Agent mode v IDE** – Copilot autonomně provádí změny ve více souborech.
- **Copilot CLI** – agentní práce přímo z terminálu, tvorba PR z příkazové řádky.
- **Cloud agent** – plně autonomní agent přiřazený k issue nebo PR na GitHub.com.
- **Copilot code review** – automatizovaná revize kódu bez nutnosti lidského reviewera.

## Copilot jako LLM provider pro třetí strany
- GitHub Copilot předplatné lze použít jako LLM backend v externích AI nástrojích.
- **OpenCode** – open source terminálový agent, podporuje Copilot bez nutnosti API klíče.
- Stejně podporováno: ChatGPT Plus, GitLab Duo – Copilot je součástí ekosystému.
- Výhoda: jedno předplatné pokrývá jak GitHub UI, tak externí agenty a nástroje.

---

# GitHub Copilot CLI

## Instalace a konfigurace CLI
- Instalace přes GitHub CLI rozšíření (`gh extension install github/gh-copilot`) nebo standalone `copilot` CLI binary.
- Vyžaduje ověření přes `gh auth login` a platné Copilot předplatné.
- Konfigurace prostředí: nastavení schválených nástrojů, osobních hooks a skills.
- Podporované systémy: Linux, macOS, Windows (PowerShell a WSL).

## Příkazy pro generování kódu z terminálu
- Interaktivní režim: spuštění příkazem `copilot`, konverzace s agentem.
- Programatický režim: `copilot -p "zadání"` pro jednorázové spuštění a exit.
- Plánovaný režim (Plan mode): Shift+Tab – Copilot nejprve vytvoří plán, pak kód.
- Příklady: refaktoring souboru, tvorba PR, commit, vysvětlení změn v repozitáři.

## Integrace CLI do skriptů a CI/CD
- Pipe vstup do `copilot` pro automatizaci: `./generate-options.sh | copilot`.
- Použití `--allow-tool` pro selektivní povolení nástrojů bez manuálního schválení.
- Integrace do GitHub Actions jako krok v pipeline pro automatické úlohy.
- Kombinace s `gh` CLI pro manipulaci s issues, PR a repozitáři přímo ze skriptů.

## CLI vs. IDE – kdy co použít
- CLI preferovat pro serverové prostředí, CI/CD a dávkové operace bez GUI.
- IDE vhodné pro interaktivní vývoj s vizuální zpětnou vazbou v editoru.
- CLI umožňuje vzdálené ovládání session spuštěné v IDE (Remote Control).
- CLI agenti mohou běžet paralelně (Fleet) – IDE agenti pracují sekvenčně.

---

# IDE integrace

## Instalace a nastavení ve VS Code a Visual Studiu
- VS Code: instalace rozšíření „GitHub Copilot" a „GitHub Copilot Chat" z Marketplace.
- Visual Studio: rozšíření dostupné přímo v IDE od verze 2022 17.6+.
- Přihlášení přes GitHub účet s platnou Copilot licencí.
- Nastavení: výchozí model, jazyk odezvy, povolení nebo zakázání funkcí.

## Inline completion, chat panel a code review
- Inline completion: návrhy se zobrazují šedě přímo při psaní, přijmout klávesou Tab.
- Next edit suggestions: Copilot predikuje místo další úpravy a nabídne ji rovnou.
- Chat panel: konverzace v kontextu otevřených souborů, projektu nebo výběru kódu.
- Code review: Copilot přidává komentáře k diff v pull requestu přímo v editoru.

## Aplikace Agents ve VS Code
- VS Code obsahuje dedikovanou aplikaci **Agents** přístupnou přes ikonu Copilot v activity baru nebo záložku v Copilot Chat panelu.
- Nabízí galerii dostupných agentů – veřejných, firemních i vlastních (custom).
- Agenti se dělí podle režimu: **Ask** (pouze odpovídá), **Edit** (upravuje soubory), **Agent** (autonomní, používá nástroje).
- Každý agent v galerii zobrazuje název, popis, dostupné nástroje a skills – uživatel si vybere agenta pro konkrétní úlohu.
- Přepínání mezi agenty přímo v chat panelu bez nutnosti restartovat editor nebo měnit konfiguraci.
- Organizace mohou publikovat vlastní firemní agenty do galerie přes GitHub Enterprise – viditelní pouze pro členy organizace.

## Kontextové okno
- Kontextové okno je maximální množství textu (tokenů), které model najednou „vidí".
- Zahrnuje: otevřené soubory, historii chatu, instrukce, výstupy nástrojů a kód projektu.
- Větší kontext = relevantnější odpovědi, ale vyšší spotřeba tokenů.
- Copilot automaticky prioritizuje nejrelevantnější části projektu do kontextového okna.

## Tokeny a účtování
- Token je základní jednotka – přibližně 4 znaky nebo jedno slovo v angličtině.
- Každá interakce spotřebovává input tokeny (dotaz) a output tokeny (odpověď).
- Cached tokeny jsou levnější – model si reusuje kontext z předchozího volání.
- Copilot Business: 1 900 AI kreditů/uživatel/měsíc; 1 AI kredit = 0,01 USD.
- Inline completion a next edit suggestions se tokenově neúčtují – jsou neomezené.

---

# Přizpůsobení Copilota

## Instrukce (Instructions)
- Soubor `.github/copilot-instructions.md` – platí pro celý repozitář.
- Osobní instrukce v nastavení VS Code – platí pro všechny projekty uživatele.
- Definují: styl kódu, preferované knihovny, architektonické konvence, jazyk odpovědí.
- Copilot je automaticky načítá do kontextu každé interakce bez nutnosti připomínání.

## Prompts
- Prompt soubory (`.prompt.md`) jsou znovupoužitelné šablony pro konkrétní úlohy.
- Uloženy v `.github/prompts/` – dostupné v celém repozitáři pro celý tým.
- Vyvolání přes chat: `/nazev-promptu` nebo výběr ze seznamu.
- Vhodné pro opakované operace: generování testů, tvorba migračních skriptů, code review.

## Custom agenti
- Vlastní agenti definovaní přes `.github/agents/*.agent.md` nebo `.copilot/agents/`.
- Každý agent má vlastní název, popis, instrukce, dostupné nástroje a skills.
- Tým může mít agenty pro různé role: backend vývojář, tester, DBA, security reviewer.
- Vyvolání agentem v chatu přes `@jméno-agenta` nebo přiřazením k issue.
- Po vytvoření souboru `.agent.md` se agent automaticky objeví v aplikaci Agents ve VS Code a je dostupný celému týmu pracujícímu ve stejném repozitáři.
- Aplikace Agents zobrazuje detail každého agenta: popis, instrukce, režim (Ask / Edit / Agent) a přiřazené nástroje – bez nutnosti otevírat konfigurační soubor.

## Skills
- Skills jsou složky s instrukcemi, skripty a zdroji pro specializované úlohy.
- Uloženy v `.github/skills/`, `.agents/skills/` nebo osobně v `~/.copilot/skills/`.
- Otevřený standard (agentskills) – sdílené skills z komunity na GitHubu.
- Instalace přes `gh skill install <repo>/<skill>` přímo z příkazové řádky.

## Hooks
- Shell skripty spouštěné v klíčových bodech práce agenta.
- Typy: `sessionStart`, `sessionEnd`, `preToolUse`, `postToolUse`, `userPromptSubmitted`, `agentStop`.
- `preToolUse` umožňuje schválit nebo zamítnout akci agenta před provedením.
- Konfigurace v `.github/hooks/*.json` (projekt) nebo `~/.copilot/hooks/*.json` (osobní).

## MCP servery
- Model Context Protocol – otevřený standard pro napojení AI na externí nástroje.
- MCP server zpřístupňuje nástroje (tools), zdroje (resources) a prompts agentům.
- Konfigurace v `.vscode/mcp.json` nebo globálně v nastavení VS Code.
- Příklady: Microsoft Learn MCP (dokumentace), GitHub MCP (repozitáře, issues, PR).

## Plugins
- Rozšíření pracovního postupu Copilot CLI o vlastní nebo třetí strany nástroje.
- Definovány jako spustitelné soubory s popisem schémat vstupů a výstupů.
- Enterprise plugin standards zajišťují schválení a auditovatelnost pluginů v organizaci.
- Vhodné pro: napojení na interní systémy, ticketing, databáze, deployment nástroje.

## Copilot jako LLM provider
- Copilot předplatné lze použít jako poskytovatele LLM v externích AI nástrojích.
- OpenCode (`opencode-ai`): terminálový AI agent s podporou Copilot bez API klíče.
- Konfigurace přes `/connect` v OpenCode – výběr GitHub Copilot jako providera.
- Umožňuje využívat modely Copilota (GPT-4o, Claude, atd.) mimo GitHub ekosystém.

---

# Architektury vhodné pro AI-first vývoj

## Principy návrhu pro spolupráci s AI
- Kód musí být čitelný pro AI – jasné názvy, malé funkce, explicitní závislosti.
- Vyhněte se „magic" konstrukcím – AI lépe pracuje s přímočarým, konzistentním kódem.
- Dokumentace jako součást kódu: XML komentáře, README, ADR pomáhají kontextu.
- Preferovat konvence před konfigurací – AI snáze dodržuje vzory než specifická pravidla.

## Modularita a čistá architektura
- Rozdělení do vrstev (Presentation, Application, Domain, Infrastructure) usnadňuje delegování.
- Malé, soudržné moduly s jasně definovaným rozhraním jsou pro agenty předvídatelnější.
- Dependency Injection umožňuje agentům generovat a testovat komponenty izolovaně.
- Vertical Slice Architecture: každá funkcionalita jako samostatný celek vhodný pro PR.

## Oddělení zodpovědností
- Single Responsibility Principle jako základ pro zadávání atomických úloh agentovi.
- Repository pattern, CQRS a mediátor (MediatR) pomáhají jasně vymezit hranice.
- Oddělení business logiky od infrastruktury usnadňuje generování unit testů.
- Jasné kontrakty (interfaces, DTOs) snižují riziko nekonzistentních výstupů agenta.

## Vzory vhodné pro delegování na agenty
- Feature branches + pull requesty: každá úloha agenta žije ve vlastní větvi.
- Conventional Commits: strukturované commit zprávy usnadňují orientaci v historii.
- OpenAPI / Swagger: API kontrakt jako vstup pro generování klientů a testů.
- Infrastructure as Code (Bicep, Terraform): agenti mohou generovat i cloudovou infrastrukturu.

---

# Delegování vývojových úloh

## Formulování zadání pro agenta
- Zadání musí obsahovat: kontext, cíl, omezení a očekávaný výstup.
- Čím konkrétnější a kratší úloha, tím předvídatelnější výsledek.
- Odkazujte na existující soubory, testy nebo konvence v repozitáři.
- Iterujte – první výsledek je výchozí bod, ne finální produkt.

## Zpracování výstupů a validace
- Vždy zkontrolujte diff před přijetím změn – agenti mohou přepsat nečekané soubory.
- Spusťte testy automaticky po každé změně agenta (hooks, CI).
- Porovnávejte výstup s původním zadáním a architektonickými konvencemi projektu.
- Používejte plan mode (Copilot CLI) pro složité úlohy – plán schválíte před implementací.

## Větvení a pull requesty řízené agentem
- Copilot cloud agent automaticky vytváří větev a otevírá PR k přiřazenému issue.
- Každý PR obsahuje popis změn, dopad a seznam upravených souborů generovaný agentem.
- Code review lze delegovat na Copilot – komentáře k PR přímo v GitHub UI.
- Workflow: Issue → přiřazení Copilotovi → PR → review → merge.

---

# Testování a kvalita kódu

## Generování unit a integračních testů
- Copilot generuje testy na základě existující implementace nebo zadané specifikace.
- Podpora xUnit, NUnit, MSTest pro .NET; Moq a NSubstitute pro mockování.
- TDD přístup: nejprve požádejte o testy, pak o implementaci splňující tyto testy.
- Integrace s .NET Test Explorer – vygenerované testy lze rovnou spustit v IDE.

## Statická analýza a refaktoring
- Copilot identifikuje code smells, duplicity a porušení principů při code review.
- Refaktoring na vyžádání: „Refaktoruj tuto metodu podle Single Responsibility Principle."
- Podpora Roslyn analyzátorů – Copilot respektuje pravidla definovaná v `.editorconfig`.
- Automatická oprava varování (warnings) z buildu jako zadání pro agenta.

## Kontinuální integrace a automatizované kontroly
- GitHub Actions workflow pro automatické spuštění testů po každém PR agenta.
- Hooks `postToolUse` a `sessionEnd` pro logování a notifikace výsledků.
- Status checks jako brána: PR nelze merge bez úspěšného buildu a testů.
- Copilot může generovat a udržovat samotné workflow soubory pro CI/CD pipeline.

---

# Praktický workshop

## Vývoj čisté .NET aplikace s agenty
- Vytvoření nového .NET projektu s čistou architekturou jako startovací bod.
- Delegování implementace jednotlivých vrstev (Domain, Application, Infrastructure) agentovi.
- Průchod kompletním workflow: issue → CLI/cloud agent → PR → review → merge.
- Diskuze nad rozhodnutími agenta a iterace na základě zpětné vazby.

## Práce s vzorovým repozitářem
- Vzorový repozitář obsahuje předpřipravené instrukce, prompty a skills pro .NET vývoj.
- Demonstrace reálné struktury `.github/` složky: hooks, agents, prompts, skills.
- Přidání vlastní instrukce a okamžité ověření dopadu na odpovědi Copilota.
- Ukázka konfigurace MCP serverů a jejich využití při vývoji.

## Reálné scénáře z praxe
- Přidání nové feature (CRUD endpoint) od issue po nasazení s pomocí agenta.
- Oprava bugu: přiřazení issue Copilotovi, sledování PR a validace výsledku.
- Migrace starší .NET aplikace – Copilot jako asistent při modernizaci kódu.
- Měření produktivity před a po zavedení Copilot agentů v týmu.

---

# Bezpečnost a governance

## Ochrana citlivých dat a secrets
- Copilot nikdy neodesílá kód do veřejného modelu bez souhlasu – Enterprise garancí.
- Secret scanning hook (`preToolUse`) zabraňuje commitování přihlašovacích údajů.
- Content exclusion: definujte soubory a složky, které Copilot nikdy nesmí číst ani upravovat.
- Při práci s production daty vždy ověřte, že agent nemá přímý přístup k databázi.

## Firemní politiky přes GitHub Enterprise
- Administrátoři mohou zapnout nebo vypnout jednotlivé funkce Copilota na úrovni organizace.
- Politika MCP serverů: povolení nebo zákaz použití externích MCP serverů pro uživatele.
- Politika pro Copilot CLI, cloud agent a third-party agenty – granulární řízení přístupu.
- GitHub Enterprise Cloud umožňuje geografickou datovou rezidenci pro regulované odvětví.

## Audit log a sledování využití
- Každá akce Copilot agenta je zaznamenána v GitHub Audit Logu organizace.
- Usage metriky: accepted completions, AI credit consumption, aktivní uživatelé.
- Hooks `sessionStart`/`sessionEnd` pro vlastní logování na úrovni projektu.
- Budgety na úrovni uživatele, cost center a enterprise pro kontrolu nákladů.

---

# Práce s GitHub Issues a projekty

## Delegování issues na Copilot coding agent
- Přiřaďte issue Copilotovi stejně jako člověku – přes GitHub UI nebo `gh` CLI.
- Agent prozkoumá repozitář, vytvoří implementační plán a otevře PR.
- Lze zadat vlastní instrukce přímo v komentáři issue pro upřesnění zadání.
- Vhodné pro dobře definované, ohraničené úlohy s jasným výstupem.

## Sledování průběhu a schvalování výstupů
- Průběh práce agenta je viditelný v záložce PR – živý log akcí a změn.
- Agent žádá o schválení před citlivými operacemi (spuštění příkazů, merge).
- Iterace: přidejte komentář do PR a agent automaticky zapracuje zpětnou vazbu.
- Výsledný PR prochází standardním review procesem – lidský schvalovatel je zachován.

---

# Prompt engineering pro vývojáře

## Zásady efektivního zadávání instrukcí
- Buďte konkrétní: uveďte technologii, vzor, omezení a požadovaný výstup.
- Poskytněte kontext: přiložte relevantní soubory, příklady nebo existující kód.
- Rozdělte složité úlohy na kroky – agent pracuje lépe s atomickými zadáními.
- Používejte imperativní formulace: „Vytvoř", „Refaktoruj", „Přidej test pro".

## Iterace nad výsledky a ladění promptů
- První výsledek vnímejte jako návrh – doptejte se na alternativy nebo upřesnění.
- Pokud výsledek neodpovídá, upravte prompt a přidejte příklad požadovaného výstupu.
- Používejte plan mode pro složité úlohy – zkontrolujte plán před spuštěním.
- Uložte osvědčené prompty jako `.prompt.md` soubory pro opakované použití týmem.

## Jednorázový dotaz vs. řízený agent
- Jednorázový dotaz (chat): rychlá otázka, vysvětlení kódu, návrh refaktoringu.
- Řízený agent: autonomní splnění úlohy zahrnující více souborů, testů a PR.
- Volte agenta pro úlohy trvající více než 5 minut nebo zahrnující více než 3 soubory.
- Kombinace: nejprve chatový dotaz pro ověření přístupu, pak delegování agentovi.
