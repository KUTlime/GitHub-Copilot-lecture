# ADR 0002: No mediator library

- Status: Accepted
- Date: 2026-10-11

## Context

Mediator libraries (MediatR, Wolverine, source-generated Mediator) decouple endpoints from handlers through a
dispatcher. MediatR became commercial in 2025. The indirection (`IRequest` → DI → handler → pipeline behaviors)
also makes "go to definition" harder for humans and agents.

## Decision

Each slice exposes a static `HandleAsync` method that its nested `Endpoint` calls directly. Cross-cutting concerns
are handled by ASP.NET Core itself: validation by an endpoint filter (`ValidationFilter<T>`), errors by `Result` mapping
and ProblemDetails, telemetry by OpenTelemetry.

## Consequences

- The call chain of a request is fully visible in one file.
- No license or package dependency for dispatching.
- Pipeline-style behaviors (e.g. transactions around every command) would need explicit code if required later.
