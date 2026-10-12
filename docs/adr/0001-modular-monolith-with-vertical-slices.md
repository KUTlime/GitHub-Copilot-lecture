# ADR 0001: Modular monolith with vertical slices

- Status: Accepted
- Date: 2026-10-11

## Context

The repository is a reference for AI-assisted development. Agents work best when a typical change touches few,
cohesive files and when boundaries are explicit and machine-checkable. Layered architectures (Clean/Onion/Hexagonal)
spread a single feature over several projects and folders; microservices spread it over several repositories and add
network and distributed-transaction concerns.

## Decision

- One deployable application, split into business modules (`Catalog`, `Orders`, `Shipping`), each with an internal
  project and a public `*.Contracts` project.
- Inside a module, code is organized by use case: `Features/<Slice>/<Slice>.cs`. Business rules live in `Domain/`.
- Boundaries are enforced by the compiler (`internal`), by `Directory.Build.targets` and by architecture tests.

## Consequences

- A typical feature is one new slice file plus tests in one module (good locality for humans and agents).
- Some glue code (DTOs, small helpers) is duplicated between slices on purpose instead of sharing a "Common" project.
- Modules can later be extracted into services along the existing contracts if ever needed.
