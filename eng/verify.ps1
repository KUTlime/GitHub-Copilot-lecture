#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Definition of Done for AgentShop: solution sync, build with zero warnings, format, tests.

.DESCRIPTION
    Runs every quality gate in the same order as CI. The full run requires Docker (integration tests use
    Testcontainers). -SkipIntegration runs only the fast tier; it is NOT the full Definition of Done.

.EXAMPLE
    pwsh eng/verify.ps1
    pwsh eng/verify.ps1 -SkipIntegration
#>
[CmdletBinding()]
param(
    [switch] $SkipIntegration
)

$ErrorActionPreference = 'Stop'
Set-Location (Join-Path $PSScriptRoot '..')

function Invoke-Step([string] $Name, [scriptblock] $Command) {
    Write-Host "==> $Name" -ForegroundColor Cyan
    & $Command
    if ($LASTEXITCODE -ne 0) {
        Write-Host "FAILED: $Name" -ForegroundColor Red
        exit $LASTEXITCODE
    }
}

Invoke-Step 'Solutions in sync (AgentShop.sln generated from AgentShop.slnx)' { dotnet run eng/sync-solutions.cs -- --check }
Invoke-Step 'Restore local tools' { dotnet tool restore }
Invoke-Step 'Build AgentShop.slnx (warnings are errors)' { dotnet build AgentShop.slnx }
Invoke-Step 'Build generated AgentShop.sln' { dotnet build AgentShop.sln --no-restore }
Invoke-Step 'Format (no changes allowed)' { dotnet format AgentShop.slnx --verify-no-changes --no-restore }

if ($SkipIntegration) {
    Invoke-Step 'Tests: fast tier (no Docker)' { dotnet test --solution AgentShop.slnx --no-build -- --filter-not-trait "Category=Integration" }
    Write-Host 'Integration tests were skipped. This is NOT the full Definition of Done.' -ForegroundColor Yellow
}
else {
    Invoke-Step 'Tests: all tiers (requires Docker)' { dotnet test --solution AgentShop.slnx --no-build }
    Write-Host 'Definition of Done: OK' -ForegroundColor Green
}
