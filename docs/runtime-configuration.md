# Runtime Configuration

`alloyed-devops-multitool` resolves runtime configuration with explicit precedence:

1. Environment variables
2. `config/appsettings.yml` (or `config/appsettings.yaml`)
3. `config/appsettings.json`
4. Built-in defaults

## Environment variable format

Preferred prefix:

- `ALLOYED__Section__Subsection__Key`

Migration compatibility prefix (also supported):

- `TAF__Section__Subsection__Key`

Console output mode uses a separate flat key (see [Console reporter mode](#console-reporter-mode)):

- `ALLOYED_CONSOLE_OUTPUT_MODE`

Examples:

```powershell
$env:ALLOYED__RUNTIME__FAILONSEVERITY = "Warning"
$env:ALLOYED__RUNTIME__DEFAULTOUTPUTPATH = "generated-modules"
$env:ALLOYED__SESSION__ENABLED = "true"
$env:ALLOYED__MOCKING__ENABLED = "true"
$env:ALLOYED__MOCKING__MODE = "InMemory"
$env:ALLOYED__DECORATION__ENABLETRANSPARENCY = "true"
$env:ALLOYED__CATALOG__SOURCEPATH = "tools/ports/ports.catalog.json"
$env:ALLOYED_CONSOLE_OUTPUT_MODE = "Rich"   # single underscore — PS-layer only
```

## Keys reference

| Key | Type | Default | Notes |
|---|---|---|---|
| `Alloyed:Runtime:FailOnSeverity` | `Info\|Warning\|Error` or empty | empty | When set, pipeline fails if diagnostics meet/exceed threshold. |
| `Alloyed:Runtime:DefaultOutputPath` | string | `out` | Used by `New-AlloyedModuleTransform` when `-OutputPath` is omitted. Can be relative to current directory or absolute. |
| `Alloyed:Session:Enabled` | bool | `false` | Session mode feature flag (behavior delivered in next wave). |
| `Alloyed:Decoration:EnableErrorHandling` | bool | `true` | Decorator runtime flag. |
| `Alloyed:Decoration:EnableObservability` | bool | `true` | Decorator runtime flag. |
| `Alloyed:Decoration:EnableCorrelation` | bool | `true` | Decorator runtime flag. |
| `Alloyed:Decoration:EnableTransparency` | bool | `false` | Watch/transparency mode flag (next wave). |
| `Alloyed:Mocking:Enabled` | bool | `false` | Enables mock mode controls. |
| `Alloyed:Mocking:Mode` | `InMemory\|Moq\|Custom` | `InMemory` | Throws actionable error on invalid value. |
| `Alloyed:Catalog:SourcePath` | string or empty | empty | Optional path to external `ports.catalog.json`. Empty uses embedded catalog from the assembly. |
| `ALLOYED_CONSOLE_OUTPUT_MODE` | `Plain\|Rich` | `Plain` | PS-layer only (single underscore). Selects console reporter. `Rich` falls back to `Plain` when output is redirected (CI). |

## Example `config/appsettings.yml`

```yaml
Alloyed:
  Runtime:
    FailOnSeverity: Warning
    DefaultOutputPath: generated-modules
  Session:
    Enabled: false
  Decoration:
    EnableErrorHandling: true
    EnableObservability: true
    EnableCorrelation: true
    EnableTransparency: false
  Mocking:
    Enabled: false
    Mode: InMemory
  Catalog:
    SourcePath: tools/ports/ports.catalog.json
```

## Console reporter mode

Controls whether output uses plain text or Spectre.Console rich formatting.

Resolution order (first match wins):

1. Per-call `-OutputMode Plain|Rich` parameter on `New-AlloyedModuleTransform` / `Test-AlloyedTransform` — applies only for that call, restores previous state on exit.
2. Session override set by `Enable-AlloyedTransparencyMode -OutputMode Rich` or direct `$script:ConsoleOutputModeOverride` assignment.
3. `ALLOYED_CONSOLE_OUTPUT_MODE` environment variable (`Plain` or `Rich`, case-insensitive). Note the single-underscore format — this key is not part of the `ALLOYED__` double-underscore C# config hierarchy.
4. Default: `Plain`.

`Rich` mode automatically falls back to `Plain` when `[Console]::IsOutputRedirected` is `true` (pipelines, CI runners with redirected stdout).

```powershell
# Session-wide rich output
$env:ALLOYED_CONSOLE_OUTPUT_MODE = "Rich"

# Single-call override (state restored after the call)
New-AlloyedModuleTransform -ScriptPath ./script.ps1 -ModuleName MyModule -OutputMode Rich
```

## Session mode commands

```powershell
Enable-AlloyedSessionMode
Get-AlloyedSessionModeStatus
Disable-AlloyedSessionMode
```

## Transparency watch mode

Enable from config:

```powershell
$env:ALLOYED__DECORATION__ENABLETRANSPARENCY = "true"
```

Or toggle at runtime for current session:

```powershell
Enable-AlloyedTransparencyMode [-OutputMode Plain|Rich]
Get-AlloyedTransparencyModeStatus
Disable-AlloyedTransparencyMode
```

When enabled, wrappers emit sanitized watch events through `TransparencyDecorator`.
Sensitive tags (for example keys containing `password`, `secret`, `token`, `apikey`, `credential`) are redacted.
