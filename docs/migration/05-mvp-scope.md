# MVP Scope: alloyed-devops-multitool

> **Status: DELIVERED** — All acceptance criteria met. Iterations 0–5 complete (2026-04-25).

## MVP Goal

Deliver one complete vertical slice proving the concept:
AST-driven transformation + generated wrapper call + decorated execution path in a portable PowerShell module.

## Delivered (MVP)

### AST transform path

- Parse script and detect supported command calls.
- Replace selected commands with wrapper mappings deterministically.
- Implementation: `HeuristicScriptAnalyzer` (text-based, dependency-free bridge).

### Minimal builder pipeline

- `TextCommandTransformer` — regex-based replacement respecting strings, comments, here-strings.
- `MinimalModuleBuilder` — emits `.psm1`, `.psd1`, `README.md`.
- Generated outputs are stable and golden-tested.

### Decoration runtime

- Own pipeline implementation (no Castle.DynamicProxy).
- Three decorators: ErrorHandling, Observability, Correlation.
- Priority-based ordering, single-owner exception policy.

### Portable module assembly

- Module imports and executes on PowerShell 7+.
- Manifest follows portable module conventions.

### Tests

- Unit: catalog mappings, text transformer edge cases (quotes, comments, here-strings), decorator chain ordering.
- Integration: `TransformationPipeline` end-to-end orchestration.
- Smoke: module import + catalog exposure + transformed command execution.

## Acceptance Criteria — Result

- [x] Sample script with `Get-ChildItem` transformed into wrapper usage.
- [x] Generated module imports and executes on PowerShell 7+.
- [x] Decorator chain is configurable and order-verified by tests.
- [x] Build and tests run in CI on Windows + Linux.
- [x] Documentation describes extension points for builders and decorators.

## Safety and Cost Guardrails (Still Active)

- No new dependencies without explicit approval.
- Each feature addition after MVP ships in isolated increments.

## Post-MVP Roadmap

See `03-phased-migration-plan.md` for the full phase plan. Summary order:

1. CI/CD hardening — Jenkins in Podman, replace Pester with container integration tests.
1. System ports expansion — six port groups, Utility first.
1. Real PowerShell AST — when heuristic is insufficient.
1. Catalog and config maturation — dynamic source, minimal settings.
1. Additional decorators — Validation, Caching on demand.
1. Publishing — PSGallery / NuGet when all system port groups covered.
