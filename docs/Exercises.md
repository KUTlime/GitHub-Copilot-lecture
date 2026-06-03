# Cvičení ke kurzu: Vývoj .NET aplikací s podporou GitHub Copilot

---

## Cvičení 1: První interakce s Copilot Chat

**Cíl:** Seznámit se s Copilot Chat panelem a naučit se formulovat dotazy.

**Zadání:**
1. Otevřete VS Code s nainstalovaným rozšířením GitHub Copilot.
2. Otevřete Copilot Chat panel (Ctrl+Alt+I).
3. Položte Copilotovi následující dotazy a porovnejte výstupy:
   - „Vysvětli rozdíl mezi `record` a `class` v C#."
   - „Napiš mi jednoduchý příklad použití `record` pro doménový objekt."
   - „Refaktoruj předchozí příklad tak, aby používal immutable properties."
4. Všimněte si, jak Copilot využívá kontext předchozí konverzace.

**Očekávaný výstup:** Pochopení konverzačního rozhraní a vlivu kontextu na odpovědi.

---

## Cvičení 2: Inline completion v praxi

**Cíl:** Vyzkoušet si automatické návrhy kódu při psaní.

**Zadání:**
1. Vytvořte nový soubor `Product.cs` a začněte psát:
   ```csharp
   public class Product
   {
       public int Id { get; set; }
       public string Name { get; set; }
       // pokračujte psaním dalších properties...
   }
   ```
2. Sledujte, jaké návrhy Copilot nabízí (šedý text).
3. Přijměte návrhy klávesou Tab nebo je odmítněte klávesou Esc.
4. Vytvořte třídu `ProductService` a začněte psát metodu `GetProductById` – nechte Copilot navrhnout implementaci.
5. Vyzkoušejte psaní komentáře nad metodou (např. `// Vrátí všechny produkty seřazené podle ceny`) a sledujte, jak komentář ovlivní návrh.

**Očekávaný výstup:** Praktická zkušenost s inline completion a pochopení vlivu kontextu (názvy, komentáře) na kvalitu návrhů.

---

## Cvičení 3: Vytvoření projektových instrukcí

**Cíl:** Nastavit chování Copilota pro konkrétní projekt pomocí instrukcí.

**Zadání:**
1. V kořenu repozitáře vytvořte složku `.github/` (pokud neexistuje).
2. Vytvořte soubor `.github/copilot-instructions.md` s následujícím obsahem:
   ```markdown
   # Instrukce pro Copilot

   - Vždy používej C# 12 a .NET 8.
   - Preferuj record typy pro DTOs a value objects.
   - Používej nullable reference types (NRT).
   - Testy piš v xUnit s knihovnou FluentAssertions.
   - Odpovídej v češtině.
   ```
3. Otevřete Copilot Chat a požádejte: „Vytvoř DTO pro objednávku s položkami."
4. Ověřte, že Copilot respektuje vaše instrukce (record, NRT, čeština).
5. Přidejte další instrukci dle vlastní preference a ověřte její dopad.

**Očekávaný výstup:** Funkční soubor instrukcí, který prokazatelně ovlivňuje chování Copilota.

---

## Cvičení 4: Tvorba prompt šablony

**Cíl:** Vytvořit znovupoužitelný prompt pro opakovanou úlohu.

**Zadání:**
1. Vytvořte složku `.github/prompts/` v repozitáři.
2. Vytvořte soubor `.github/prompts/create-endpoint.prompt.md`:
   ```markdown
   ---
   description: "Vytvoří nový CRUD endpoint pro zadanou entitu"
   ---
   Vytvoř kompletní CRUD endpoint pro entitu "{{entity}}":
   1. Controller s akcemi GET (list + detail), POST, PUT, DELETE.
   2. Request a Response DTOs jako record typy.
   3. Service interface a jeho implementaci.
   4. Registraci v DI kontejneru.

   Dodržuj konvence projektu definované v copilot-instructions.md.
   ```
3. V Copilot Chatu vyvolejte prompt a zadejte entitu (např. „Customer").
4. Zkontrolujte, zda výstup odpovídá šabloně a instrukcím projektu.

**Očekávaný výstup:** Fungující prompt šablona, kterou může používat celý tým pro konzistentní generování kódu.

---

## Cvičení 5: Generování unit testů

**Cíl:** Využít Copilota pro automatické generování testů k existujícímu kódu.

**Zadání:**
1. Vytvořte třídu `PriceCalculator`:
   ```csharp
   public class PriceCalculator
   {
       public decimal CalculateDiscount(decimal price, int quantity)
       {
           if (quantity >= 100) return price * 0.8m;
           if (quantity >= 50) return price * 0.9m;
           if (quantity >= 10) return price * 0.95m;
           return price;
       }
   }
   ```
2. Označte celou třídu a v Copilot Chatu zadejte: „Vygeneruj kompletní unit testy pro tuto třídu v xUnit. Pokryj všechny hraniční hodnoty."
3. Zkontrolujte vygenerované testy – pokrývají všechny větve?
4. Požádejte o doplnění: „Přidej testy pro zápornou cenu a nulové množství."
5. Spusťte testy a ověřte, že procházejí.

**Očekávaný výstup:** Kompletní sada unit testů s pokrytím hraničních hodnot, vygenerovaná Copilotem.

---

## Cvičení 6: Copilot CLI – základní práce

**Cíl:** Vyzkoušet si práci s GitHub Copilot v terminálu.

**Zadání:**
1. Ověřte instalaci: `gh copilot --version` (nebo `copilot --version`).
2. Spusťte interaktivní režim a zadejte:
   - „Vysvětli, co dělá poslední commit v tomto repozitáři."
   - „Vytvoř .gitignore pro .NET projekt."
3. Vyzkoušejte programatický režim:
   ```bash
   copilot -p "Vytvoř shell skript, který spustí dotnet test a vypíše souhrn výsledků"
   ```
4. Vyzkoušejte plan mode (Shift+Tab v interaktivním režimu):
   - Zadejte: „Přidej do projektu health check endpoint na /health."
   - Prohlédněte si plán a schvalte nebo upravte jej.

**Očekávaný výstup:** Praktická zkušenost s CLI režimy a pochopení rozdílů mezi nimi.

---

## Cvičení 7: Delegování issue na Copilot coding agent

**Cíl:** Vyzkoušet si přiřazení úlohy cloud agentovi přes GitHub Issues.

**Zadání:**
1. V repozitáři na GitHubu vytvořte nový issue s názvem:
   „Přidej endpoint GET /api/status vracející JSON s verzí aplikace a časem serveru"
2. V popisu issue uveďte:
   - Požadovaný response formát (JSON příklad).
   - Odkaz na existující controller jako vzor.
   - Požadavek na unit test.
3. Přiřaďte issue uživateli „Copilot" (nebo použijte `gh` CLI).
4. Sledujte záložku PR – agent vytvoří větev a otevře pull request.
5. Proveďte review výsledného PR:
   - Odpovídá implementace zadání?
   - Jsou testy přítomny a smysluplné?
   - Pokud ne, přidejte komentář a sledujte, jak agent reaguje.

**Očekávaný výstup:** Kompletní workflow issue → agent → PR → review s reálným výstupem.

---

## Cvičení 8: Konfigurace MCP serveru

**Cíl:** Napojit Copilota na externí zdroj dat přes MCP protokol.

**Zadání:**
1. Vytvořte soubor `.vscode/mcp.json` v projektu:
   ```json
   {
     "servers": {
       "github": {
         "command": "npx",
         "args": ["-y", "@modelcontextprotocol/server-github"],
         "env": {
           "GITHUB_PERSONAL_ACCESS_TOKEN": "${input:github-token}"
         }
       }
     }
   }
   ```
2. Restartujte VS Code a ověřte, že MCP server je aktivní (ikona v status baru).
3. V Copilot Chatu vyzkoušejte dotaz využívající MCP:
   - „Jaké issues jsou otevřené v repozitáři [váš-repo]?"
   - „Shrň poslední 3 pull requesty."
4. Porovnejte odpověď s a bez MCP serveru – jaký je rozdíl v kvalitě?

**Očekávaný výstup:** Funkční MCP konfigurace a pochopení, jak externí nástroje rozšiřují schopnosti Copilota.

---

## Cvičení 9: Code review s Copilotem

**Cíl:** Využít Copilot pro automatizovanou revizi kódu.

**Zadání:**
1. Vytvořte novou větev a přidejte záměrně problematický kód:
   ```csharp
   public class UserService
   {
       private string connectionString = "Server=prod;Password=admin123;";

       public async Task<User> GetUser(int id)
       {
           // SQL injection vulnerability
           var sql = $"SELECT * FROM Users WHERE Id = {id}";
           // ...
       }

       public void ProcessUsers(List<User> users)
       {
           for (int i = 0; i < users.Count(); i++) // performance issue
           {
               Console.WriteLine(users[i].Name);
           }
       }
   }
   ```
2. Otevřete PR z této větve.
3. Požádejte o Copilot code review (tlačítko v PR nebo `@copilot` v komentáři).
4. Projděte komentáře Copilota:
   - Identifikoval SQL injection?
   - Identifikoval hardcoded credentials?
   - Identifikoval performance issue s `Count()`?
5. Opravte nalezené problémy na základě doporučení.

**Očekávaný výstup:** Pochopení schopností a limitů automatického code review.

---

## Cvičení 10: Custom agent pro tým

**Cíl:** Vytvořit vlastního agenta se specializovanou rolí.

**Zadání:**
1. Vytvořte složku `.github/agents/` v repozitáři.
2. Vytvořte soubor `.github/agents/api-designer.agent.md`:
   ```markdown
   ---
   name: api-designer
   description: "Specialista na návrh REST API podle best practices"
   tools:
     - read_file
     - edit_file
     - grep
   ---
   Jsi API designer specialista. Tvým úkolem je:
   - Navrhovat REST API endpointy podle RESTful konvencí.
   - Kontrolovat konzistenci pojmenování a HTTP metod.
   - Navrhovat response kódy a error handling.
   - Dbát na verzování API a zpětnou kompatibilitu.

   Vždy se řiď principy:
   - Konzistentní URL schéma (plural nouns, kebab-case).
   - Správné HTTP status kódy (201 pro POST, 204 pro DELETE).
   - HATEOAS odkazy kde je to vhodné.
   - Stránkování pro kolekce.
   ```
3. V Copilot Chatu vyvolejte agenta: `@api-designer Navrhni API pro správu objednávek (orders) včetně položek (order items).`
4. Zhodnoťte kvalitu návrhu a iterujte.

**Očekávaný výstup:** Funkční custom agent s jasně definovanou rolí, použitelný celým týmem.

---

## Bonusové cvičení: Měření produktivity

**Cíl:** Porovnat rychlost práce s a bez Copilota.

**Zadání:**
1. Vyberte jednoduchou úlohu (např. implementace CRUD repository pro entitu).
2. Změřte čas implementace bez Copilota (vypněte rozšíření).
3. Změřte čas stejné úlohy (jiná entita) s Copilotem.
4. Porovnejte:
   - Čas implementace.
   - Počet řádků kódu.
   - Počet chyb při prvním spuštění testů.
   - Subjektivní pocit z práce.
5. Diskutujte ve skupině: Kde Copilot nejvíce pomohl? Kde překážel?

**Očekávaný výstup:** Kvantifikovaný přehled přínosu Copilota pro konkrétní typ úlohy.
