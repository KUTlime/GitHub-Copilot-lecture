# ADR 0006: InternalsVisibleTo only to the module's own test project

- Status: Accepted
- Date: 2026-10-11

## Context

Everything in a module project is `internal` so that other modules cannot depend on it. Domain invariants are best
tested directly on the aggregates, which are internal. Kamil Grzybek's modular-monolith reference argues against
`InternalsVisibleTo` because it weakens encapsulation.

## Decision

`Directory.Build.targets` adds exactly one `InternalsVisibleTo` to each module project: `AgentShop.Modules.<Module>.Tests`.
No other project (other modules, the API host, integration tests) can see module internals. Integration tests are
black-box HTTP tests with their own DTOs.

## Consequences

- Fast, precise unit tests of aggregates and validators without making them public.
- Unit tests can couple to internal implementation details; keep them focused on behavior (aggregate methods,
  validator rules), not on private structure.
- Adding `InternalsVisibleTo` anywhere else is a violation of this ADR.
