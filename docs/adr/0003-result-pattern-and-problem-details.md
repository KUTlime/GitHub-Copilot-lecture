# ADR 0003: Result pattern and ProblemDetails

- Status: Accepted
- Date: 2026-10-11

## Context

Exceptions for expected failures (not found, conflict, invalid input) hide possible outcomes from method signatures.
Clients need one consistent error format.

## Decision

- Expected failures are returned as `Result`/`Result<T>` carrying an `Error(Code, Description, Type)`.
- Field-level failures use `ValidationError` (a dictionary of field → messages) so that domain validation and
  FluentValidation produce the same `400 ValidationProblem` shape with keys like `Lines[0].ProductId`.
- `ResultExtensions` maps errors centrally: NotFound → 404, Conflict → 409, Validation → 400, Failure → 500,
  all as RFC 9457 problem details. Validation messages are always English (FluentValidation localization is disabled).
- Exceptions are reserved for unexpected failures and are handled by `UseExceptionHandler` + `AddProblemDetails`.

## Consequences

- Handlers and aggregates have explicit outcomes; the compiler helps to handle them.
- A small amount of ceremony (`if (result.IsFailure) return result.Error;`) instead of throwing.
