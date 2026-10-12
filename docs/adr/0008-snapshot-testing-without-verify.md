# ADR 0008: Approval tests with an in-repo JsonSnapshot helper instead of Verify

- Status: Accepted
- Date: 2026-10-11

## Context

Snapshot (approval) tests of HTTP responses and of the OpenAPI document are a key guardrail: any contract change is
visible as a diff in the pull request. The original plan was to use Verify (`Verify.XunitV3`).

Verify 33.x fails the build with `SC021` unless the consuming project declares a sponsorship, a license period or a
publisher-defined exemption (open source, small revenue, ...). Such a declaration is a legal statement about the
organization using the repository, which cannot be made on its behalf by a developer or an agent. The alternative
Snapshooter targets xUnit v3 3.x (this repository uses 4.x) and depends on Newtonsoft.Json, which is banned here.

## Decision

Use a small helper `tests/AgentShop.Api.IntegrationTests/Infrastructure/JsonSnapshot.cs` (System.Text.Json only):

- approved snapshots in `Snapshots/<Class>.<Method>.approved.json` (committed),
- on mismatch a git-ignored `*.received.json` is written and the test fails with the paths to compare,
- GUIDs are replaced with `Guid_N`, `traceId` is removed; timestamps are deterministic thanks to `TestClock`,
- an intended change is accepted with `AGENTSHOP_ACCEPT_SNAPSHOTS=1` (ignored when `CI=true`).

## Consequences

- No license obligation and no extra dependency; ~120 lines of test infrastructure to maintain.
- Fewer features than Verify (no diff tool integration, JSON only).
- If the organization decides to sponsor/license Verify, the helper can be replaced by `VerifyJson` with the same
  scrubbing rules (`ScrubInlineGuids`, ignore `traceId`).
- Lesson for agent-driven work: checking a package's license terms is part of "verify the package exists".
