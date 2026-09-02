# Iteration Backlog: alloyed-devops-multitool

> **Historical document — not maintained.** This backlog records what was planned, and its statuses
> have since drifted from the implementation (see `docs/migration-status-matrix.md`, which notes
> Milestone 7 still marked "triggered" here although the work is complete). Treat the code, the
> ADRs and the status matrix as authoritative.

## Completed Iterations (MVP)

### Iteration 0 — Scaffold ✓

- Create solution/projects skeleton by blueprint.
- Wire basic references and test projects.

Exit: repository structure ready for feature work.

### Iteration 1 — AST Foundation ✓

- Implement `IScriptAnalyzer` as `HeuristicScriptAnalyzer` (text-based, dependency-free).
- Emit `ScriptAnalysisResult` and command usages.

Exit: deterministic analysis output for sample scripts.

### Iteration 2 — Catalog + Resolution ✓

- Implement `IWrapperCatalog` as `InMemoryWrapperCatalog`.
- Initial command set: `Get-ChildItem`, `Get-Item`, `Test-Path` + aliases.

Exit: reliable replacement map consumable by transformer.

### Iteration 3 — Transform + Builder MVP ✓

- `TextCommandTransformer`: regex-based, respects strings/comments/here-strings.
- `MinimalModuleBuilder`: emits `.psm1`, `.psd1`, `README.md`.

Exit: end-to-end file generation from one sample script.

### Iteration 4 — Decoration Runtime ✓

- Own `DecorationPipeline` (no Castle.DynamicProxy).
- Three decorators: ErrorHandling, Observability, Correlation.
- Priority ordering, single-owner exception policy.

Exit: decorated execution path proven in tests.

### Iteration 5 — Host.PowerShell and Smoke ✓

- `ITransformationPipeline` orchestration (`TransformationPipeline`).
- Cmdlets: `New-AlloyedModuleTransform`, `Test-AlloyedTransform`, `Get-AlloyedCatalog`.
- CI: Windows + Linux jobs via GitHub Actions.

Exit: MVP vertical slice complete.

---

## Milestone 1 — Governance and Archival

Status: **ready to start.**

### Task 1.1 — Write alloy specification

Write `ALLOYING_SPEC.md` in the alloyed-devops-multitool project root.

Sections: Base Material, Alloying Components, Target Properties, Cost Model, Acceptance Evidence.

Checks:

- Every core component maps to a concrete source file or contract.
- Acceptance criteria are verifiable (not aspirational).

### Task 1.2 — Archive TAF repository

Archive the TAF repository on GitHub (Settings → Archive repository).

Checks:

- Repository shows "Archived" badge.
- No further commits possible to TAF.

### Task 1.3 — Migrate TAF samples

Copy representative TAF test scripts into `samples/` in alloyed-devops-multitool as transformation input fixtures.

Checks:

- Samples transform without errors using current pipeline.
- At least one golden fixture updated to use a TAF-originated script.

Exit: alloy spec approved, TAF archived, samples in place.

---

## Milestone 2 — CI/CD: pre-push hook + PR-only GHA

Status: **complete.**

### Task 2.1 — GHA trigger on PR only ✓

Removed `push:` trigger from `.github/workflows/ci.yml`. Workflow now fires only on `pull_request` and `workflow_dispatch`.

Checks:

- `push` to any branch does not trigger GHA jobs.
- PRs trigger full pipeline: restore → build → unit → integration → smoke.

### Task 2.2 — Pre-push hook ✓

Created `.githooks/pre-push` (bash entry point) and `.githooks/pre-push.ps1` (logic).

Hook stages: build (no restore) → `dotnet format --verify-no-changes` → PSScriptAnalyzer → unit tests.

Checks:

- Hook runs without errors on a clean working tree.
- Hook fails fast on formatting violations.
- Hook fails fast on unit test failures.

### Task 2.3 — Hook installation in dev.ps1 ✓

Added `setup` stage to `dev.ps1`. Runs `git config core.hooksPath .githooks` and `chmod +x` on Linux/macOS.

Checks:

- `pwsh -NoProfile -File ./dev.ps1 -Stage setup` installs hooks without errors.
- `git push` triggers the hook.

Exit: pushes are guarded locally; GHA costs occur only on PRs.

---

## Milestone 3 — System Ports: Utility Group

Status: pending Milestone 2.

### Task 3.1 — Inventory Utility commands

List target commands from TAF `System.Utility`. Define wrapper naming for each.

Checks:

- List is explicit; no ambiguous entries.
- Wrapper names follow `Verb-AlloyedNoun` convention.

### Task 3.2 — Catalog mappings

Add Utility group entries to `InMemoryWrapperCatalog`. Include common aliases.

Checks:

- Unit tests cover all new mappings (positive + alias cases).
- Missing command detection still works for commands outside the group.

### Task 3.3 — Wrapper stubs

Generate wrapper stubs in the PowerShell module for each Utility command.

Checks:

- Each wrapper passes through to the underlying command.
- Module manifest lists all new functions.

### Task 3.4 — Golden fixtures

Add golden input/expected fixture pair covering a script that uses multiple Utility commands.

Checks:

- `TextCommandTransformer` output matches expected fixture exactly.
- CI golden test runs on both Windows and Linux.

Exit: all Utility group commands transform correctly; tests green on both platforms.

---

## Milestone 4 — System Ports: Diagnostics Group ✓

Status: **complete.**

Commands: `Get-Process`, `Start-Process`, `Stop-Process`, `Wait-Process`, `Test-Connection`, `Invoke-Command`.
Aliases: `ps`, `gps`, `saps`, `start`, `kill`, `spps`, `icm`.

Exit: Diagnostics group cataloged, wrapped in module, golden-tested. Unit tests: 18 total.

---

## Milestone 5 — System Ports: Remaining Groups ✓

Status: **complete.**

### System.Archive ✓

Commands: `Compress-Archive`, `Expand-Archive`.

### System.Management ✓

Commands: `Get-Service`, `Start-Service`, `Stop-Service`, `Restart-Service`.
Aliases: `gsv`, `sasv`, `spsv`.

### System.Security ✓

Commands: `Get-Acl`, `Set-Acl`, `Get-Credential`, `ConvertTo-SecureString`, `ConvertFrom-SecureString`, `Get-AuthenticodeSignature`, `Set-AuthenticodeSignature`, `New-SelfSignedCertificate`, `Get-PfxCertificate`, `Export-PfxCertificate`.

### System.Host ✓

Commands: `Write-Host`, `Read-Host`, `Write-Progress`, `Clear-Host`.
Aliases: `cls`, `clear`.

Exit: all six system port groups cataloged, wrapped, and golden-tested. Unit tests: 28 total.

---

## Milestone 6 — Real PowerShell AST ✓

Status: **complete.**

Trigger: `HeuristicScriptAnalyzer` regex matched only `gci|gi|tp` as single-word aliases — all 16 new aliases (measure, sort, group, ps, kill, cls, clear, saps, start, spps, icm, gsv, sasv, spsv, sls, gps) were invisible to the pipeline.

### Task 6.1 — Identify failure case ✓

Single-word aliases missed by heuristic regex `(?<cmd>[A-Za-z]+-[A-Za-z][A-Za-z0-9-]*|gci|gi|tp)`.

### Task 6.2 — Implement PS AST analyzer ✓

`PowerShellScriptAnalyzer` using `System.Management.Automation.Language.Parser.ParseInput()` + `FindAll(node => node is CommandAst)`. Added `System.Management.Automation` 7.4.6 package. Parse errors mapped as Warnings (AST still produced, transformation proceeds).

### Task 6.3 — Swap and verify ✓

`PipelineBootstrap` updated to `new PowerShellScriptAnalyzer()`. Updated `InferAstCode` to recognize PS AST message format ("missing the terminator"). Updated pipeline golden fixture and assertion to reflect `Write-Host` now being cataloged.

Checks:

- No regressions: 40 unit + 8 integration = 48 tests green.
- 12 new tests in `PowerShellScriptAnalyzerTests` covering aliases, script blocks, string/comment/here-string exclusions, module-qualified commands, line numbers, parse error diagnostics.

Exit: real PS AST in use; `HeuristicScriptAnalyzer` retained as fallback reference only.

---

## Milestone 7 — Dynamic Catalog + Minimal Config

Status: triggered when hardcoded mapping count makes additions error-prone, or when pipeline defaults need to vary by caller.

### Task 7.1 — Dynamic catalog source

Load catalog mappings from a YAML or JSON file. `InMemoryWrapperCatalog` becomes a loader, not a hardcoded class.

Checks:

- Loaded catalog matches hardcoded baseline (parity test).
- Unknown source path produces a clear, actionable error.

### Task 7.2 — Minimal configuration layer

Add a settings object for pipeline behavior: output path defaults, decorator toggles, catalog source path.

Checks:

- Config values are observable in pipeline output (at least one setting affects behavior).
- Defaults match current hardcoded behavior (no regressions).

Exit: catalog driven from file; basic settings object in place.

---

## Milestone 8 — Additional Decorators

Status: triggered when a concrete port group scenario requires validation or caching.

### Task 8.1 — ValidationDecorator

Validate command parameters before execution. Pluggable validation rules per command.

### Task 8.2 — CachingDecorator

Cache results for idempotent commands. Configurable TTL and cache key strategy.

Checks: decorator ordering tests updated; existing three-decorator chain unaffected.

Exit: four- or five-decorator chain proven in tests for at least one port group.

---

## Milestone 9 — Publishing

Status: triggered when all six system port groups are covered (Milestones 3–5 complete).

### Task 9.1 — Jenkins publish stage

Add publish stage to Jenkins pipeline: pack → push to PSGallery and/or NuGet.

### Task 9.2 — Version and manifest

Finalize semantic versioning, changelog, and module manifest metadata.

Checks:

- Module installs cleanly from gallery on a clean PS7+ instance.
- Version increments are automated from git tags.

Exit: published module available; first portfolio-ready release tagged.

---

## Rollback Model

- One milestone = one or more reversible PRs.
- No cross-milestone dependency that prevents rollback.
- Golden fixtures versioned per milestone.
- Catalog entries can be removed without breaking the pipeline for remaining entries.
