# ADR-0001: Port Architecture and Generation Contract

Status: Accepted  
Date: 2026-04-28  
Related issue: `#14`

## Context

Migration scale requires one consistent rule for:
- when ports stay wrapper-only (two-tier),
- when they must include service/port layers (three-tier),
- how generation and verification must work in CI/local flows.

Current implementation already provides:
- catalog-driven wrapper generation (`tools/ports/ports.catalog.json`),
- deterministic transformation pipeline,
- configuration and CI guards for generated artifacts.

## Decision

1. System/native command modernization uses **two-tier ports** by default.
2. External integration domains (cloud/network/data/sdk) use **three-tier ports** by default.
3. All new ports must follow one documented generation contract and checklist.

## Architecture Patterns

## Two-tier port (wrapper-first)

Use when:
- operation is direct PowerShell/native command mapping,
- no domain orchestration/retry/policy logic is required beyond decorators,
- objective is deterministic modernization of existing scripts.

Structure:
- Cmdlet/wrapper function (PowerShell)
- Catalog mapping (`command -> wrapper -> native`)

Example domains:
- `Microsoft.PowerShell.Management`
- `Microsoft.PowerShell.Utility`
- `Microsoft.PowerShell.Security`

## Three-tier port (service + port)

Use when:
- integration is with external SDK/API/system,
- domain validation, retries, transformations, or orchestration are needed,
- operations need behavior beyond simple passthrough.

Structure:
- PowerShell cmdlet surface
- Service interface + implementation
- Port interface + implementation (SDK boundary)
- Optional decorators at service boundary

Example domains:
- AWS, HTTP, SQL, vendor SDK modules

## Generation Contract

## Input source of truth

- `tools/ports/ports.catalog.json` is canonical for wrapper catalog entries.

## Generated artifacts

- `src/powershell/Alloyed.DevOps.Multitool.psm1` (wrapper functions)
- `src/powershell/Alloyed.DevOps.Multitool.psd1` (exports)

## Runtime mapping

- .NET catalog mapping loads from the same JSON source (embedded resource by default; optional external source path).

## Invariants

1. Regeneration is idempotent.
2. CI/dev fails if generated files diverge from catalog.
3. Wrapper naming follows `Verb-AlloyedNoun`.
4. Alias collisions are explicitly resolved in catalog review.

## Migration Mapping (Current -> Target)

| Current state | Target pattern |
|---|---|
| Existing wrapper-only system ports | Keep as two-tier |
| New popular native modules | Two-tier first |
| New cloud/network/data integrations | Three-tier |
| Existing two-tier requiring domain policies | Promote to three-tier in dedicated migration wave |

## Port Wave Checklist

For each new port wave:

1. Decide pattern (`two-tier` or `three-tier`) and record rationale.
2. Update catalog or add service/port contracts accordingly.
3. Regenerate artifacts (`Sync-PortsFromCatalog.ps1`) if two-tier.
4. Add unit/integration coverage for mappings and behavior.
5. Run smoke and CI-equivalent checks.
6. Update docs (`README`, migration matrix, relevant ADR/contract docs).

## Starter Templates

## Two-tier starter

1. Add entry to `tools/ports/ports.catalog.json`:
- `command`
- `wrapper`
- `native`
- `aliases`
2. Run:
- `pwsh -NoProfile -File tools/ports/Sync-PortsFromCatalog.ps1`
3. Verify:
- `dotnet test ...Unit...`
- `dotnet test ...Integration...`
- `pwsh -NoProfile -File tests/powershell/Smoke.Module.Tests.ps1`

## Three-tier starter

1. Add contracts:
- `I<Domain>Service`
- `I<Domain>Port`
2. Add implementations:
- `<Domain>Service`
- `<Domain>Port`
3. Add host wiring + cmdlet surface.
4. Add tests:
- service unit tests
- integration tests
- smoke path where applicable
5. Update versioning/contract docs for new stable surface.

## Consequences

Positive:
- predictable migration path,
- lower ambiguity for new contributors,
- one verification standard for generated artifacts.

Tradeoff:
- some integrations may start with two-tier and later need promotion to three-tier.

## Compliance

A PR is compliant with this ADR when:
- chosen pattern is explicit,
- generation contract is followed,
- required checks pass,
- docs are updated with any new stable API surface.

