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

Examples:

```powershell
$env:ALLOYED__RUNTIME__FAILONSEVERITY = "Warning"
$env:ALLOYED__RUNTIME__DEFAULTOUTPUTPATH = "generated-modules"
$env:ALLOYED__SESSION__ENABLED = "true"
$env:ALLOYED__MOCKING__ENABLED = "true"
$env:ALLOYED__MOCKING__MODE = "InMemory"
$env:ALLOYED__DECORATION__ENABLETRANSPARENCY = "true"
$env:ALLOYED__CATALOG__SOURCEPATH = "tools/ports/ports.catalog.json"
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
Enable-AlloyedTransparencyMode
Get-AlloyedTransparencyModeStatus
Disable-AlloyedTransparencyMode
```

When enabled, wrappers emit sanitized watch events through `TransparencyDecorator`.
Sensitive tags (for example keys containing `password`, `secret`, `token`, `apikey`, `credential`) are redacted.
