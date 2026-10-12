---
applyTo: "**/*.props,**/*.targets,**/*.csproj,**/*.sln,**/*.slnx,global.json,nuget.config"
description: "Build infrastructure is a guardrail: do not change it without an explicit request."
---
# Build infrastructure

- `Directory.Build.props`, `Directory.Build.targets`, `Directory.Packages.props`, `global.json` and `nuget.config` are guardrails.
  Do NOT change them unless the task explicitly asks for it.
- Package versions live only in `Directory.Packages.props` (Central Package Management). `PackageReference` has no `Version`.
- A new package needs a justification in the PR, must exist on nuget.org (check with the NuGet MCP server) and have an acceptable license.
- A new project goes only into `AgentShop.slnx` (`dotnet sln AgentShop.slnx add <csproj>`); then regenerate the classic
  solution with `dotnet run eng/sync-solutions.cs`. Never edit `AgentShop.sln` by hand.
- Keep `.csproj` files minimal: shared settings come from `Directory.Build.props` (and `tests/Directory.Build.props`).
