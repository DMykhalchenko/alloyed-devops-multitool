# alloyed-devops-multitool

[![CI](https://github.com/Ligare-Method/alloyed-devops-multitool/actions/workflows/ci.yml/badge.svg)](https://github.com/Ligare-Method/alloyed-devops-multitool/actions/workflows/ci.yml)
[![Integration+Publish](https://github.com/Ligare-Method/alloyed-devops-multitool/actions/workflows/integration-publish.yml/badge.svg)](https://github.com/Ligare-Method/alloyed-devops-multitool/actions/workflows/integration-publish.yml)

`alloyed-devops-multitool` is a PowerShell modernization toolkit:

- run legacy scripts through decorators without renaming commands,
- transform scripts into wrapper-based modules,
- control runtime behavior with explicit configuration.

## Why This Exists

Many teams have production PowerShell scripts that are hard to observe, risky to refactor, and expensive to rewrite.
This module provides a low-friction migration path:

1. keep original command names,
2. add transparency/decorators,
3. evolve toward generated modules and explicit ports.

## Quick Start

Prerequisites:

- PowerShell 7+
- .NET 8 SDK

Import module:

```powershell
Import-Module ./src/powershell/Alloyed.DevOps.Multitool.psd1 -Force
```

Run a legacy script with decorators in one command:

```powershell
Invoke-AlloyedScript -ScriptPath ./scripts/legacy.ps1
```

Manual mode (if you want full session control):

```powershell
Enable-AlloyedTransparencyMode
./scripts/legacy.ps1
Disable-AlloyedTransparencyMode
```

## Execution Flow

```mermaid
flowchart LR
    A["Import-Module"] --> B["Enable-AlloyedTransparencyMode"]
    B --> C["Original command names stay unchanged"]
    C --> D["Decorated runtime execution"]
    D --> E["Structured transparency output"]
    D --> F["Retry/Backoff/Timeout policy"]
```

## Architecture

```mermaid
flowchart TB
    S["Legacy Script (.ps1)"] --> M["PowerShell Module (alloyed-devops-multitool)"]
    M --> P["Session interception (original command names)"]
    P --> R["Invoke-AlloyedCommandRuntime"]
    R --> D["Decoration Pipeline"]
    D --> T["TransparencyDecorator"]
    D --> C["CorrelationDecorator"]
    D --> O["ObservabilityDecorator"]
    D --> E["ErrorHandlingDecorator"]
    M --> X["Transform Pipeline (AST + Catalog + Builder)"]
```

## Public Command Surface

Primary runtime commands:

- `Invoke-AlloyedScript`
- `Enable-AlloyedTransparencyMode`
- `Disable-AlloyedTransparencyMode`
- `Get-AlloyedTransparencyModeStatus`

Configuration commands:

- `Initialize-AlloyedRuntimeConfig`
- `Test-AlloyedRuntimeConfig`

Transform commands:

- `New-AlloyedModuleTransform`
- `Test-AlloyedTransform`
- `Get-AlloyedCatalog`
- `Get-AlloyedRuntimeConfiguration`

## Runtime Policy

Tune runtime behavior with environment variables:

- `ALLOYED_RUNTIME_MAX_RETRIES` (default `0`)
- `ALLOYED_RUNTIME_RETRY_DELAY_SEC` (default `2`)
- `ALLOYED_RUNTIME_EXPONENTIAL_BACKOFF` (`true|false`, default `false`)
- `ALLOYED_RUNTIME_PREVIEW` (`true|false`, default `false`)
- `ALLOYED_RUNTIME_TIMEOUT_SEC` (default `0`, disabled)
- `ALLOYED_CONSOLE_OUTPUT_MODE` (`Plain|Rich`, default `Plain`)
- `ALLOYED_TRANSPARENCY_VERBOSE` (`true|false`, default `false`)

Timeout behavior:

- hard timeout is applied for non-pipeline wrapper calls,
- pipeline-input calls use safe fallback without hard cancellation.

## Script Transformation

Transform a script into a generated module:

```powershell
New-AlloyedModuleTransform `
  -ScriptPath ./samples/sample-transform-input.ps1 `
  -ModuleName DemoAlloyed `
  -OutputPath ./temp/out `
  -Force
```

Validate transform only:

```powershell
Test-AlloyedTransform -ScriptPath ./samples/sample-transform-input.ps1
```

## Migration Roadmap View

```mermaid
flowchart LR
    A["Legacy Script Execution"] --> B["Transparency + Decorators"]
    B --> C["Runtime Policy Hardening"]
    C --> D["Port Coverage Expansion"]
    D --> E["Module Generation at Scale"]
    E --> F["Sandbox & Scenario Modeling"]
```

## Validation

Run local CI-equivalent:

```powershell
pwsh -NoProfile -File ./dev.ps1 -Stage ci
```

Targeted stages:

```powershell
pwsh -NoProfile -File ./dev.ps1 -Stage unit
pwsh -NoProfile -File ./dev.ps1 -Stage integration
pwsh -NoProfile -File ./dev.ps1 -Stage smoke
```

## Documentation

- Installation from GitHub Packages: `docs/install-module.md`
- Runtime configuration: `docs/runtime-configuration.md`
- Legacy transparency quickstart: `docs/legacy-transparency-quickstart.md`
- Module access model: `docs/module-access-model.md`
- Contracts and versioning: `docs/contracts-and-versioning.md`
- Port architecture ADR: `docs/adr-0001-port-architecture-and-generation-contract.md`
- Spectre reporting ADR: `docs/adr-0002-spectre-console-reporting-boundary.md`
- Delivery policy: `docs/delivery-policy.md`
- Migration governance: `docs/migration-governance.md`
- Migration status matrix: `docs/migration-status-matrix.md`
- Alloying spec: `ALLOYING_SPEC.md`

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

MIT. See `LICENSE`.

## Release Pipeline

A low-frequency integration + publish pipeline is available in `.github/workflows/integration-publish.yml`:

- Weekly scheduled run (`Sunday 03:30 UTC`)
- Manual trigger (`workflow_dispatch`)
- Publishes preview module to GitHub Packages only after successful integration tests
