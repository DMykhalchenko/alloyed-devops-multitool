# Contracts and Versioning Policy

Last updated: 2026-04-28

## Goal

Define stable public contracts and versioning/deprecation rules for core modules:
- `Core.Ast`
- `Core.Catalog`
- `Core.Builders`
- `Core.Decoration`
- `Host.PowerShell`

## Public Contract Surface

### .NET interfaces (stable extension points)

- `IScriptAnalyzer`
- `IWrapperCatalog`
- `ICommandTransformer`
- `IModuleBuilder`
- `IDecorationPipeline`
- `IDecorationSink`
- `IDecorator`
- `IDecoratorPolicy`
- `ITransformationPipeline`

Contract rule:
- Changes to method signatures, required behaviors, or model semantics are considered contract changes.

### PowerShell public commands

Primary user-facing commands:
- `New-AlloyedModuleTransform`
- `Test-AlloyedTransform`
- `Get-AlloyedCatalog`
- `Get-AlloyedRuntimeConfiguration`
- `Enable-AlloyedSessionMode`
- `Disable-AlloyedSessionMode`
- `Get-AlloyedSessionModeStatus`
- `Enable-AlloyedTransparencyMode`
- `Disable-AlloyedTransparencyMode`
- `Get-AlloyedTransparencyModeStatus`

Generated wrapper functions from `tools/ports/ports.catalog.json` are treated as public module API once exported in `.psd1`.

## Stability Levels

| Surface | Stability |
|---|---|
| .NET interfaces listed above | `Stable` |
| PowerShell primary commands | `Stable` |
| Generated wrapper set and names | `Stable` (catalog-driven) |
| Internal helper functions/classes not exported or not in `Contracts` namespaces | `Internal` |

## Semantic Versioning Policy

Version format: `MAJOR.MINOR.PATCH[-prerelease]`.

- `PATCH`: bug fixes, diagnostics clarity, internal refactors with no public contract change.
- `MINOR`: backward-compatible additions (new wrapper commands, new optional config keys, new non-breaking cmdlet parameters).
- `MAJOR`: breaking changes to stable contracts (interface signature changes, removed/renamed exported commands, incompatible output/behavior changes).

## Deprecation Policy

1. Mark deprecated API in docs and changelog.
2. Keep deprecated API for at least one `MINOR` cycle.
3. Emit warning path where practical (PowerShell warning or diagnostic).
4. Remove only in next `MAJOR`.

For generated wrappers:
- Prefer alias/compat mapping first.
- Remove wrapper names only with explicit major-release notice.

## Change Control Checklist

Every PR affecting stable contracts must include:

1. Contract impact statement:
- `none`, `additive`, or `breaking`.

2. Test evidence:
- Unit + integration + smoke passing.
- If wrapper catalog changed: regeneration check and parity tests pass.

3. Documentation updates:
- Update this policy if stability level or rules changed.
- Update README/runtime docs if user-facing behavior changed.

## Current Baseline

This policy reflects implementation status on/after:
- `f2d798d` (catalog loaded from JSON source)
- `a81bfa1` (runtime config additions for output path/catalog source)
- `19fb9a9` (alloy spec refresh)

