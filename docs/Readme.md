# Osnova kurzu

> Rámcový obsah kurzu.

## Obsah

* Vzájemné představení
* Zmapování znalostí .NET, AI technologií a GitHub Copilota
* Zmapování pracovních postupů
* Seznámení s repositářem kurzu
* Licenční politika GitHub Copilot
* Seznámení s repositáři
* Rešerše známých poznatků ohledně AI-first architektury
* Ukázka implementace fíčury s pomocí AI agenta
* DDD (Description Driven Development) a SDD (Specs Driven Development)
* Obojky pro AI agenty
* Diskuze nad vlastními projekty/problémy

## Srovnání kontextových oken stejných modelů podle různých harness

**Srovnání kontextového okna Claude Sonnet / Opus – GitHub Copilot vs. Claude Code (předplatné Anthropic), stav k 2026-10-12**

| Model | GitHub Copilot – vstup (prompt) | GitHub Copilot – celé okno (vstup + výstup) | Claude Code (předplatné Anthropic) | Max. výstup Copilot / Anthropic |
|---|---|---|---|---|
| Claude Opus 5.5 | 200k | 328k | 1M (nativně) | 128k / 128k |
| Claude Opus 5 | 200k | 264k | 1M (nativně) | 64k / 128k |
| Claude Opus 4.8 | 200k | 264k | 1M (nativně) | 64k / 128k |
| Claude Sonnet 5.5 | 200k | 328k | 1M (nativně) | 128k / 128k |
| Claude Sonnet 5 | 200k | 264k | 1M (nativně) | 64k / 128k |

Poznámky: Copilot = hodnoty `max_prompt_tokens` a `max_context_window_tokens` z API `api.githubcopilot.com/models`. Anthropic = dokumentace Claude API a Claude Code. Okno 1M je u těchto modelů výchozí bez příplatku za dlouhý kontext a lze ho vypnout přes `CLAUDE_CODE_DISABLE_1M_CONTEXT=1`.

## Obojky pro AI agenty

* EditorConfig
* MS Build
* NSwag
* Public API
* Git hooky
* BannedApi NuGet
* Test Driven Development
* CI/CD dry run
* Sonar


## Odkazy

* [How large is the context window on paid Claude plans](https://support.claude.com/en/articles/8606394-how-large-is-the-context-window-on-paid-claude-plans)
