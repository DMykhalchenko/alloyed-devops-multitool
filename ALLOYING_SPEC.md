# ALLOYING_SPEC — alloyed-devops-multitool

Last updated: 2026-04-28

## 1. Goal

Freeze Phase 1 migration scope and define conformance checks for the alloying model implemented in this repository.

Concrete reference scenario:
- Input script contains `Get-ChildItem` and `Test-Path`.
- Transformation replaces them with wrapper commands.
- Generated module executes through the decorator pipeline.

## 2. Base Material

- PowerShell script text (`.ps1`) as transformation input.
- Stable command catalog source (`tools/ports/ports.catalog.json`).
- .NET contracts and services in:
  - `src/dotnet/Alloyed.DevOps.Multitool.Core.Ast`
  - `src/dotnet/Alloyed.DevOps.Multitool.Core.Catalog`
  - `src/dotnet/Alloyed.DevOps.Multitool.Core.Builders`
  - `src/dotnet/Alloyed.DevOps.Multitool.Core.Decoration`
  - `src/dotnet/Alloyed.DevOps.Multitool.Host.PowerShell`

## 3. Alloying Components

1. AST extraction:
- `PowerShellScriptAnalyzer` (PS parser-based).
- Property gain: reliable command discovery including aliases and diagnostics.

2. Catalog resolution:
- `InMemoryWrapperCatalog` loading mappings from JSON (embedded by default; optional external source path).
- Property gain: deterministic mapping, scalable maintenance.

3. Transformation:
- `TextCommandTransformer` applies token-safe replacement (protects quotes/comments/here-strings).
- Property gain: deterministic output with fixture-based regression control.

4. Module build:
- `MinimalModuleBuilder` emits `.psm1`, `.psd1`, `README.md`.
- Property gain: repeatable module artifact set.

5. Runtime decoration:
- `DecorationPipeline` with `ErrorHandling`, `Observability`, `Correlation`, `Transparency`.
- Property gain: cross-cutting behavior without contaminating wrapper logic.

6. Host orchestration:
- `TransformationPipeline` + PowerShell cmdlets (`New-AlloyedModuleTransform`, `Test-AlloyedTransform`, `Get-AlloyedCatalog`).
- Property gain: single execution path for local and CI validation.

## 4. Invariants

1. Same input script + same catalog must produce deterministic transformed output.
2. Unsupported commands must remain explicit in diagnostics (`MissingCommands`/pipeline diagnostics).
3. Catalog-driven generation must be idempotent (`tools/ports/Sync-PortsFromCatalog.ps1`).
4. CI/local validation must fail on generated artifact drift.
5. Runtime configuration defaults must be explicit and testable.

## 5. Cost Model

- Chosen tradeoff: maintain one canonical JSON catalog and generate wrappers/exports from it.
- Avoided cost: manual parallel edits in multiple files for each new port.
- Accepted cost: regeneration + parity checks in CI/dev flow.
- Deferred cost (not in Phase 1): Validation/Caching decorators and Bogus sandbox integration.

## 6. Acceptance Checks (Testable)

Conformance requires all checks below:

1. Unit:
- `dotnet test tests/dotnet/Alloyed.DevOps.Multitool.Tests.Unit/Alloyed.DevOps.Multitool.Tests.Unit.csproj -c Release -m:1 -p:BuildInParallel=false`

2. Integration:
- `dotnet test tests/dotnet/Alloyed.DevOps.Multitool.Tests.Integration/Alloyed.DevOps.Multitool.Tests.Integration.csproj -c Release -m:1 -p:BuildInParallel=false`

3. Smoke:
- `pwsh -NoProfile -File tests/powershell/Smoke.Module.Tests.ps1`

4. Generated artifact guard:
- `pwsh -NoProfile -File tools/ports/Sync-PortsFromCatalog.ps1`
- No diff in:
  - `src/powershell/Alloyed.DevOps.Multitool.psm1`
  - `src/powershell/Alloyed.DevOps.Multitool.psd1`

5. CI policy:
- `.github/workflows/ci.yml` includes generated ports verification and cross-platform build/test/smoke coverage.

## 7. Scenario Evidence (Get-ChildItem Path)

Input:
```powershell
$items = Get-ChildItem -Path ./src -Recurse
$exists = Test-Path ./out
```

Expected transformed semantics:
```powershell
$items = Get-AlloyedChildItem -Path ./src -Recurse
$exists = Test-AlloyedPath ./out
```

Runtime expectation:
- With `Enable-AlloyedTransparencyMode`, command execution emits decorator watch enter/exit logs.

## 8. Out of Scope for This Spec

- Final release governance and maintenance policy (`#11`).
- Bogus sandbox scenarios (`#9`).
- Extended decorator set (`ValidationDecorator`, `CachingDecorator`).

## 9. References

- `docs/migration-status-matrix.md`
- `docs/runtime-configuration.md`
- `README.md`
- `.github/workflows/ci.yml`
