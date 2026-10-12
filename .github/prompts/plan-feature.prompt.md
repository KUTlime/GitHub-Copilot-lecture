---
description: 'Plan a new feature for AgentShop as a spec in docs/specs/ without writing any code.'
agent: 'agent'
argument-hint: 'Describe the feature in one or two sentences'
---

# Plan a feature (no code)

Feature request: ${input:feature:Describe the feature}

Read `AGENTS.md`, `src/Modules/AGENTS.md`, `docs/architecture.md` and the relevant `docs/adr/` first.
Then create `docs/specs/<kebab-case-feature-name>.md` with these sections and nothing else:

1. **Goal** – one paragraph, user-visible outcome.
2. **Affected modules** – which modules change and why; confirm no new cross-module dependency other than via `*.Contracts`.
3. **Slices** – for each new/changed slice: name (`<Verb><Noun>`), HTTP method and route, request/response fields, status codes.
4. **Domain changes** – aggregate methods, invariants, new errors.
5. **Contracts and events** – new/changed module interface members and integration events (publisher, consumers, idempotency).
6. **Persistence** – entity/mapping changes, migrations, constraints; flag anything destructive.
7. **Tests** – unit tests (domain, validators) and integration tests (happy path + failures); expected OpenAPI snapshot change.
8. **Risks and open questions.**
9. **Tasks** – an ordered checklist of small, independently verifiable steps (one slice/migration per task),
   each ending with the command that proves it is done.

Do not modify any file other than the new spec.
