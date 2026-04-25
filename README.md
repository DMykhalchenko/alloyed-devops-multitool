# alloyed-devops-multitool

.NET-first platform for AST-driven PowerShell transformation and decorator-enabled execution.

## Iteration 0 Status
- Solution scaffold created (`Alloyed.DevOps.Multitool.slnx`).
- Core projects scaffolded:
  - `Alloyed.DevOps.Multitool.Core.Ast`
  - `Alloyed.DevOps.Multitool.Core.Builders`
  - `Alloyed.DevOps.Multitool.Core.Decoration`
  - `Alloyed.DevOps.Multitool.Core.Catalog`
  - `Alloyed.DevOps.Multitool.Host.PowerShell`
- Test placeholders scaffolded:
  - `Alloyed.DevOps.Multitool.Tests.Unit`
  - `Alloyed.DevOps.Multitool.Tests.Integration`
  - `tests/powershell`

## Iteration 1 Progress
- Added AST contracts:
  - `IScriptAnalyzer`
  - `ScriptAnalysisResult`
  - `CommandUsage`
  - `ParseDiagnostic`
- Added first analyzer implementation: `HeuristicScriptAnalyzer`.
- Current implementation is dependency-free and deterministic, intended as a bridge until full PowerShell AST integration is approved.

## Iteration 2 Progress
- Added catalog contracts and model:
  - `IWrapperCatalog`
  - `ResolutionResult`
- Added deterministic in-memory catalog implementation:
  - `InMemoryWrapperCatalog`
- Initial wrapper map includes `Get-ChildItem` family:
  - `Get-ChildItem -> Get-AlloyedChildItem`
  - `Get-Item -> Get-AlloyedItem`
  - `Test-Path -> Test-AlloyedPath`

## Iteration 3 Progress
- Added transform and builder contracts:
  - `ICommandTransformer`
  - `IModuleBuilder`
  - `ModuleBuildRequest`
  - `ModuleBuildResult`
- Added transform implementation:
  - `TextCommandTransformer` (stable command replacement by map)
- Added module generation implementation:
  - `MinimalModuleBuilder` (`.psm1`, `.psd1`, `README.md` output)

## Iteration 4 Progress
- Added decoration contracts:
  - `IDecoratorPolicy`
  - `IDecorator`
  - `IDecorationSink`
  - `IDecorationPipeline`
- Added decoration models:
  - `DecorationContext`
  - `DecorationEvent`
  - `DecorationExecutionException`
- Added pipeline implementation:
  - `DecorationPipeline`
  - `NullDecorationSink`
- Added core decorators:
  - `ErrorHandlingDecorator` (single-owner normalization)
  - `ObservabilityDecorator` (enter/exit/error events)
  - `CorrelationDecorator` (correlation tag management)

## Iteration 5 Progress
- Added Host pipeline contracts/models:
  - `ITransformationPipeline`
  - `PipelineRequest`
  - `PipelineResult`
- Added host runtime:
  - `TransformationPipeline`
  - `PipelineBootstrap`
- Connected host to core modules (`Ast`, `Catalog`, `Builders`) via project references.
- Added PowerShell wrappers module:
  - `src/powershell/Alloyed.DevOps.Multitool.psd1`
  - `src/powershell/Alloyed.DevOps.Multitool.psm1`
- Exposed commands:
  - `New-AlloyedModuleTransform`
  - `Test-AlloyedTransform`
  - `Get-AlloyedCatalog`

## Centralized Build Baseline
- `Directory.Packages.props`
- `Directory.Build.props`
- `NuGet.config`
- `global.json`
- `.editorconfig`
- `.config/PSScriptAnalyzerSettings.psd1`
- `.markdownlint.json`

## Notes
- Test projects are currently dependency-free placeholders.
- Full test framework wiring is planned in next iteration.
- Current decoration runtime is pipeline-based and dependency-free (no external DynamicProxy package yet).

## Validation Snapshot (2026-03-05)
- `dotnet restore Alloyed.DevOps.Multitool.slnx` succeeded.
- `dotnet build Alloyed.DevOps.Multitool.slnx -c Debug --no-restore` succeeded.

## Container Build
Build from repository root:

```powershell
docker build -f projects/alloyed-devops-multitool/Containerfile -t alloyed-devops-multitool:dev .
```

Run build validation inside container:

```powershell
docker run --rm alloyed-devops-multitool:dev
```

## Compose
Run containerized build with Compose:

```powershell
docker compose -f projects/alloyed-devops-multitool/compose.yaml build
docker compose -f projects/alloyed-devops-multitool/compose.yaml run --rm build
```

## End-to-End Smoke
Run the full local smoke scenario:

```powershell
pwsh -NoProfile -File projects/alloyed-devops-multitool/tests/powershell/Smoke.Module.Tests.ps1
```

This verifies:
- module import,
- catalog exposure,
- script transformation,
- generated module output content.

## CI
Workflow:
- `.github/workflows/alloyed-devops-multitool-ci.yml`

It runs:
- native build + smoke on Windows and Linux,
- container smoke on Ubuntu via `compose.yaml`.

## Unit test status
- `tests/dotnet/Alloyed.DevOps.Multitool.Tests.Unit` is now active (xUnit).
- Current baseline: 5 passing tests (Ast, Catalog, Builders, Decoration).

Run:

```powershell
dotnet test projects/alloyed-devops-multitool/tests/dotnet/Alloyed.DevOps.Multitool.Tests.Unit/Alloyed.DevOps.Multitool.Tests.Unit.csproj -c Debug
```

## Integration test status
- `tests/dotnet/Alloyed.DevOps.Multitool.Tests.Integration` is active (xUnit).
- Current baseline: 2 passing integration tests for `TransformationPipeline`.

Run:

```powershell
dotnet test projects/alloyed-devops-multitool/tests/dotnet/Alloyed.DevOps.Multitool.Tests.Integration/Alloyed.DevOps.Multitool.Tests.Integration.csproj -c Debug
```

## Temporary Jenkins CI (Podman Remote)
Use template pipeline:
- `projects/alloyed-devops-multitool/Jenkinsfile.podman-remote`

This pipeline builds and runs the project container on remote connection `centos10-root`.

Reference runbook:
- `docs/temporary-ci-podman-remote.md`

## Fast Dev Loop
Use local helper script for repeatable, fast inner-loop commands:

```powershell
# first run (with restore)
pwsh -NoProfile -File projects/alloyed-devops-multitool/dev.ps1 -Stage fast -Restore

# fast default loop (unit tests)
pwsh -NoProfile -File projects/alloyed-devops-multitool/dev.ps1

# targeted runs
pwsh -NoProfile -File projects/alloyed-devops-multitool/dev.ps1 -Stage integration
pwsh -NoProfile -File projects/alloyed-devops-multitool/dev.ps1 -Stage full
pwsh -NoProfile -File projects/alloyed-devops-multitool/dev.ps1 -Stage ci

# optional xUnit filter
pwsh -NoProfile -File projects/alloyed-devops-multitool/dev.ps1 -Stage unit -Filter "FullyQualifiedName~TransformationPipeline"
```

The script configures `DOTNET_CLI_HOME` to `projects/alloyed-devops-multitool/.dotnet-cli` to avoid machine-global state and keep runs deterministic.

`-Stage ci` mirrors the GitHub Actions execution order (`restore -> build -> unit -> integration -> smoke`) for local pre-push validation.

## GitHub Repo Bootstrap (CI/CD + Copilot)
To initialize this project as a standalone GitHub repository:

```powershell
# from monorepo root
pwsh -NoProfile -File projects/alloyed-devops-multitool/bootstrap-github.ps1 `
  -Owner <github-user-or-org> `
  -RepoName alloyed-devops-multitool `
  -CreateRemote `
  -Push
```

What gets prepared for GitHub:
- Standalone workflow: `projects/alloyed-devops-multitool/.github/workflows/ci.yml`
- Copilot repository guidance: `projects/alloyed-devops-multitool/.github/copilot-instructions.md`
