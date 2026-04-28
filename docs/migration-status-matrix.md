# Migration Status Matrix

Last updated: 2026-04-28

Purpose: single source of truth for migration status versus plan in `20260304/taf-migration`.

Status legend:
- `Done`
- `In progress`
- `Not started`

## Summary

| Area | Status | Notes |
|---|---|---|
| MVP Iterations 0-6 | Done | Vertical slice + AST + expanded system ports + tests are complete. |
| Milestone 7.1 Dynamic catalog source | Done | Catalog now loaded from `ports.catalog.json` (embedded/resource or external path). |
| Milestone 7.2 Minimal config layer | Done | Runtime config now includes default output path and catalog source path. |
| Milestone 8 Decorators expansion | Not started | Validation/Caching decorators are not implemented yet. |
| Milestone 9 Publishing convergence | In progress | Preview publish workflow exists; final governance/release policy still open. |
| Phase 1 governance docs closure | In progress | Matrix complete; alloy spec/policy issues remain open. |

## Plan vs Reality Matrix

| Planned item (20260304) | Current status | Evidence |
|---|---|---|
| Iteration 0 Scaffold | Done | Solution and project structure under `src/dotnet`, `src/powershell`, `tests`. |
| Iteration 1 AST Foundation | Done | `PowerShellScriptAnalyzer` and analyzer tests. |
| Iteration 2 Catalog + Resolution | Done | `InMemoryWrapperCatalog`, catalog tests, parity tests. |
| Iteration 3 Transform + Builder | Done | `TextCommandTransformer`, `MinimalModuleBuilder`, fixtures and integration tests. |
| Iteration 4 Decoration Runtime | Done | `Core.Decoration` pipeline and smoke visibility logs. |
| Iteration 5 Host.PowerShell + Smoke | Done | Cmdlets + integration/smoke pipelines. |
| Iteration 6 Real PS AST | Done | Parser-based command discovery and diagnostics. |
| Milestone 2 CI/CD hardening | Done | PR-only CI, pre-push hooks, local `dev.ps1` stages. |
| Milestone 3-5 system ports waves | Done | System command coverage expanded via `tools/ports/ports.catalog.json`. |
| Milestone 7.1 Dynamic catalog source | Done | `InMemoryWrapperCatalog` reads JSON source (embedded or path). |
| Milestone 7.2 Minimal config layer | Done | `Alloyed:Runtime:DefaultOutputPath`, `Alloyed:Catalog:SourcePath`. |
| Milestone 8 decorators (Validation/Caching) | Not started | No implementation/tests yet. |
| Milestone 9 publish completion | In progress | Preview publish flow is present; completion criteria/policy pending. |

## Gaps and Contradictions

| Gap | Status | Action |
|---|---|---|
| Backlog doc `20260304/taf-migration/08-iteration-backlog.md` still marks Milestone 7 as "triggered", while implementation is completed | Open documentation gap | Update backlog statuses in migration docs. |
| README previously referenced outdated test counts (fixed in latest commits) | Resolved | Keep baseline synced after each major test-count change. |
| Governance artifacts for closure (alloy spec, completion criteria) are still open in GitHub issues | Open | Complete issues `#2`, `#11`, `#6`. |

## Follow-up Issues and Milestones

| Issue | Scope | Milestone |
|---|---|---|
| `#14` | Port architecture decision and generation contract | M3 |
| `#9` | Bogus sandbox integration | M3 |
| `#10` | CI/CD convergence (GHA + optional Jenkins runbook) | M4 |
| `#11` | Migration completion criteria and maintenance policy | M4 |
| `#6` | Public contracts and versioning policy | M2/M3 |
| `#2` | Alloy spec draft/approval | M1 |

## Verification Baseline (2026-04-28)

- `dotnet test tests/dotnet/Alloyed.DevOps.Multitool.Tests.Unit/Alloyed.DevOps.Multitool.Tests.Unit.csproj -c Release -m:1 -p:BuildInParallel=false` -> passed (`50`)
- `dotnet test tests/dotnet/Alloyed.DevOps.Multitool.Tests.Integration/Alloyed.DevOps.Multitool.Tests.Integration.csproj -c Release -m:1 -p:BuildInParallel=false` -> passed (`15`)
- `pwsh -NoProfile -File tests/powershell/Smoke.Module.Tests.ps1` -> passed

## Governance Artifacts

- Alloy spec: `ALLOYING_SPEC.md`
- Contract and versioning policy: `docs/contracts-and-versioning.md`
