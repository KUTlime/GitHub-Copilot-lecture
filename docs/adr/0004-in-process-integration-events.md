# ADR 0004: In-process integration events (no outbox)

- Status: Accepted (deliberate simplification of the sample)
- Date: 2026-10-11

## Context

Modules must react to each other without referencing each other's internals (Orders → Shipping → Orders).

## Decision

- Integration events are `sealed record`s in the publisher's `*.Contracts` project and implement `IIntegrationEvent`.
- `InProcessIntegrationEventPublisher` (scoped) resolves all `IIntegrationEventHandler<TEvent>` from the current DI
  scope and calls them sequentially. Events are published **after** the publisher's `SaveChangesAsync`.
- Handlers are registered explicitly in `Add<Module>Module` and must be idempotent (unique constraint or state check).

## Consequences

- Simple, synchronous and easy to debug; no broker needed.
- **No atomicity between the commit and the publication**: if the process dies after the commit, the event is lost.
  A failing handler makes the publishing request fail although its own data is already committed.
- Production-grade alternative: a transactional outbox with a background dispatcher (e.g. Wolverine/Marten or a
  custom outbox table per module). The handler idempotency already in place is a prerequisite for that.
