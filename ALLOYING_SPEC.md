# ALLOYING_SPEC — alloyed-devops-multitool

Compliant with [Ligare Method](../../METHOD.md) — documents the deliberate composition of patterns into a base construct to achieve specific properties.

## 1. Base Material

Two constructs form the base:

- **PowerShell script text** — raw `.ps1` file content as a string. Stable, widely understood, zero-dependency starting point.
- **.NET service interface contracts** — C# `interface` definitions that describe command behavior without binding to any implementation. Stable, portable, testable by design.

These are not invented here. They are the simplest working primitives that allow improvement.

## 2. Target Properties

Properties to achieve (vocabulary from [ALLOYING_MODEL.md](../../ALLOYING_MODEL.md)):

- **Determinism** — same input script always produces the same transformed output.
- **Composability** — catalog entries, decorators, and builders extend independently without friction.
- **Testability** — each layer is validatable in isolation; the pipeline is validatable end-to-end.
- **Cross-platform integrity** — PowerShell 7+ portable module behavior on Windows and Linux.
- **Observability** — runtime pipeline emits inspectable events; decorator chain is auditable.

## 3. Alloying Components

### 3.1 Heuristic command extractor (`Core.Ast`)

Purpose: scan script text and extract command names and positions.

| | |
|---|---|
| **Gains** | Determinism (parse once, stable result), Testability (pure function, golden-testable) |
| **Cost** | Limited expressiveness — breaks for splatting, dynamic calls, complex script-blocks |
| **Applied because** | Dependency-free bridge sufficient for the current command catalog scope |
| **Upgrade trigger** | When the transformer produces incorrect output for a concrete real-world script |
| **Planned upgrade** | `System.Management.Automation.Language.Parser` (Phase 5 of migration plan) |

### 3.2 In-memory command catalog (`Core.Catalog`)

Purpose: map source command names to generated wrapper names; detect unmapped commands.

| | |
|---|---|
| **Gains** | Determinism (stable, case-insensitive mapping), Composability (add entries without touching transform logic) |
| **Cost** | Hardcoded — adding entries requires a code change |
| **Applied because** | Command count is small enough that a code-defined map is clearer than a config file |
| **Upgrade trigger** | When entry count makes inline maintenance error-prone |
| **Planned upgrade** | External source: YAML/JSON file or attribute-driven loading (Phase 6) |

### 3.3 Text command transformer (`Core.Builders — TextCommandTransformer`)

Purpose: rewrite command names in script text using the catalog replacement map.

| | |
|---|---|
| **Gains** | Determinism (regex-based, boundary-aware replacement), Testability (golden file verification) |
| **Cost** | Text-level only — respects string literals and comments, but not deep AST structure |
| **Applied because** | Sufficient for current catalog scope; simpler than AST rewriting until proven insufficient |
| **Upgrade path** | Replaced or backed by real AST transformer when heuristic limit is reached (see 3.1) |

### 3.4 Module builder (`Core.Builders — MinimalModuleBuilder`)

Purpose: assemble `.psm1`, `.psd1`, and `README.md` from a `ModuleBuildRequest`.

| | |
|---|---|
| **Gains** | Composability (builder takes a request record — caller controls all inputs), Testability (output is file content — straightforward to assert), Determinism (same request → same output) |
| **Cost** | More types; justified when output has multiple artifacts with explicit invariants |
| **Applied because** | Module assembly has three distinct artifacts and stable naming conventions; builder enforces them |

### 3.5 Own decoration pipeline (`Core.Decoration`)

Purpose: run services through an ordered chain of cross-cutting decorators without polluting business logic.

| | |
|---|---|
| **Gains** | Composability (decorators compose by priority; add or remove without changing the pipeline), Testability (test each decorator in isolation), Observability (enter/exit/error events via `IDecorationSink`) |
| **Cost** | Explicit composition required — no auto-proxying of arbitrary interfaces; each decorated path must be wired |
| **Applied because** | Castle.DynamicProxy was TAF's approach; own pipeline avoids the external dependency and the async-interceptor complexity found there |
| **Current decorators** | `ErrorHandlingDecorator`, `ObservabilityDecorator`, `CorrelationDecorator` |
| **Deferred decorators** | `ValidationDecorator`, `CachingDecorator` — added when a concrete port group scenario demands them |

### 3.6 Pipeline orchestration (`Host.PowerShell — TransformationPipeline`)

Purpose: coordinate the full `analyze → resolve → transform → assemble` sequence and expose it as PowerShell cmdlets.

| | |
|---|---|
| **Gains** | Composability (each stage is independently testable and replaceable), Testability (integration tests exercise the full sequence in-process) |
| **Cost** | Indirection — callers cannot see stage internals without instrumentation |
| **Applied because** | Separating orchestration from stage implementations keeps each layer single-responsibility |

### 3.7 Golden file tests

Purpose: assert that transformer output is byte-for-byte stable across changes.

| | |
|---|---|
| **Gains** | Determinism validation — any regression in output text is caught immediately |
| **Cost** | Fixtures need updating when output format intentionally changes |
| **Applied because** | Text transformation is hard to assert with logic alone; a known-good snapshot is the clearest specification |

## 4. Composition Constraints

Active constraints that prevent over-alloying:

- No external dynamic proxy library. Own pipeline only.
- No new runtime dependencies without explicit approval.
- Decorator minimum: 3 (Error, Observability, Correlation). Expand only for concrete scenarios.
- Catalog source: hardcoded until entry count warrants external source.
- Configuration layer: deferred until first publish milestone.
- PowerShell target: 7+ only. No `net472` or PS 5.1 support reintroduced.

## 5. Validation

| Level | What | How |
|---|---|---|
| Unit | Per-component logic (catalog mappings, transformer edge cases, decorator ordering) | xUnit + FluentAssertions |
| Golden | Transformer output stability | Fixed input fixture → fixed expected output |
| Integration | Full pipeline orchestration (in-process) | xUnit, `Tests.Integration` project |
| Container | Module import + command execution on PowerShell 7+ | Podman container, dotnet test (replacing Pester smoke) |
| CI | Cross-platform pass | GitHub Actions: Windows + Linux; Jenkins: full suite locally |

## 6. Acceptance Evidence

Checklist from [METHOD.md § 5](../../METHOD.md):

- [x] Base Material is explicitly stated (§ 1).
- [x] Alloying Components listed with property rationale (§ 3).
- [x] Properties validated with automated tests (§ 5).
- [x] Minimal usage example provided (§ 7).
- [x] No unnecessary reinvention — xUnit, FluentAssertions, .NET 8 standard patterns; no invented abstractions where ecosystem solutions exist.

## 7. Minimal Usage Example

```powershell
# transform a script and generate a portable module
New-AlloyedModuleTransform -ScriptPath ./my-script.ps1 -ModuleName MyAlloyed -OutputPath ./output

# validate transformation without writing any files
Test-AlloyedTransform -ScriptPath ./my-script.ps1

# list currently available command mappings
Get-AlloyedCatalog
```

Input (`my-script.ps1`):

```powershell
$items = Get-ChildItem -Path ./src -Recurse
$exists = Test-Path ./output
```

Output (transformed, inside generated `.psm1`):

```powershell
$items = Get-AlloyedChildItem -Path ./src -Recurse
$exists = Test-AlloyedPath ./output
```

The generated module imports `Alloyed.DevOps.Multitool` and routes all calls through the decoration pipeline (ErrorHandling → Observability → Correlation).
