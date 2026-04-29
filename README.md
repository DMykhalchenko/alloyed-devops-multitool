# alloyed-devops-multitool

[![CI](https://github.com/Ligare-Method/alloyed-devops-multitool/actions/workflows/ci.yml/badge.svg)](https://github.com/Ligare-Method/alloyed-devops-multitool/actions/workflows/ci.yml)

`alloyed-devops-multitool` transforms PowerShell scripts into wrapper-based modules and routes supported operations through a decorator pipeline.

## What it does

- Detects supported commands in PowerShell scripts.
- Rewrites supported commands to alloyed wrappers.
- Generates module artifacts (`.psm1`, `.psd1`, `README`).
- Executes wrappers through decorators (error handling, observability, correlation, transparency watch mode).

## Quick Start

Prerequisites:
- PowerShell 7+
- .NET 8 SDK

1. Import module:

```powershell
Import-Module ./src/powershell/Alloyed.DevOps.Multitool.psd1 -Force
```

2. Run an existing script with transparent decorators (no command renaming):

```powershell
Enable-AlloyedTransparencyMode
./your-legacy-script.ps1
Disable-AlloyedTransparencyMode
```

3. Transform script into module:

```powershell
New-AlloyedModuleTransform `
  -ScriptPath ./samples/sample-transform-input.ps1 `
  -ModuleName DemoAlloyed `
  -OutputPath ./temp/out `
  -Force
```

4. Validate transform only:

```powershell
Test-AlloyedTransform -ScriptPath ./samples/sample-transform-input.ps1
```

5. Inspect mapping catalog:

```powershell
Get-AlloyedCatalog
```

## Transparency Mode

```powershell
Enable-AlloyedTransparencyMode
Get-AlloyedTransparencyModeStatus
Disable-AlloyedTransparencyMode
```

When enabled, supported commands emit structured decorator watch logs for runtime visibility.
Set `ALLOYED_TRANSPARENCY_VERBOSE=true` to include an extended tag preview in transparency output.

## Validation

Run local CI-equivalent:

```powershell
pwsh -NoProfile -File ./dev.ps1 -Stage ci
```

Targeted test stages:

```powershell
pwsh -NoProfile -File ./dev.ps1 -Stage unit
pwsh -NoProfile -File ./dev.ps1 -Stage integration
pwsh -NoProfile -File ./dev.ps1 -Stage smoke
```

## Port Catalog

Wrapper generation is driven by:
- `tools/ports/ports.catalog.json`

Regenerate wrappers/exports:

```powershell
pwsh -NoProfile -File ./tools/ports/Sync-PortsFromCatalog.ps1
```

Runtime note:
- Session mode now supports dynamic wrapper bootstrap from `ports.catalog.json`. This means transparency/session usage does not require running ports sync beforehand for missing wrappers.
- Runtime execution policy can be tuned via environment variables:
- `ALLOYED_RUNTIME_MAX_RETRIES` (default `0`)
- `ALLOYED_RUNTIME_RETRY_DELAY_SEC` (default `2`)
- `ALLOYED_RUNTIME_EXPONENTIAL_BACKOFF` (`true|false`, default `false`)
- `ALLOYED_RUNTIME_PREVIEW` (`true|false`, default `false`)
- `ALLOYED_RUNTIME_TIMEOUT_SEC` (default `0`, disabled)

Timeout behavior:
- Hard timeout is applied for non-pipeline wrapper calls.
- For pipeline-input calls, runtime uses a safe fallback (no hard cancellation).

## Runtime Config API

Public commands:
- `Initialize-AlloyedRuntimeConfig`
- `Test-AlloyedRuntimeConfig`

Internal helpers (for example Spectre runtime bootstrap) are intentionally private and not part of public module API.

## Documentation

- Runtime configuration: `docs/runtime-configuration.md`
- Legacy transparency quickstart: `docs/legacy-transparency-quickstart.md`
- Contracts and versioning: `docs/contracts-and-versioning.md`
- Port architecture ADR: `docs/adr-0001-port-architecture-and-generation-contract.md`
- Spectre reporting ADR: `docs/adr-0002-spectre-console-reporting-boundary.md`
- Delivery policy: `docs/delivery-policy.md`
- Migration governance: `docs/migration-governance.md`
- Alloying spec: `ALLOYING_SPEC.md`
- Migration status matrix: `docs/migration-status-matrix.md`
- Migration archive (imported from legacy planning set): `docs/migration/`

## Repository Layout

- `src/dotnet/Alloyed.DevOps.Multitool.Core.Ast`
- `src/dotnet/Alloyed.DevOps.Multitool.Core.Catalog`
- `src/dotnet/Alloyed.DevOps.Multitool.Core.Builders`
- `src/dotnet/Alloyed.DevOps.Multitool.Core.Decoration`
- `src/dotnet/Alloyed.DevOps.Multitool.Host.PowerShell`
- `src/powershell`
- `tests/dotnet`
- `tests/powershell`

## License

This project is licensed under the MIT License. See `LICENSE`.
