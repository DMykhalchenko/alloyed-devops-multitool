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
$env:ALLOYED__SESSION__ENABLED = "true"
$env:ALLOYED__MOCKING__ENABLED = "true"
$env:ALLOYED__MOCKING__MODE = "InMemory"
```

## Keys reference

| Key | Type | Default | Notes |
|---|---|---|---|
| `Alloyed:Runtime:FailOnSeverity` | `Info\|Warning\|Error` or empty | empty | When set, pipeline fails if diagnostics meet/exceed threshold. |
| `Alloyed:Session:Enabled` | bool | `false` | Session mode feature flag (behavior delivered in next wave). |
| `Alloyed:Decoration:EnableErrorHandling` | bool | `true` | Decorator runtime flag. |
| `Alloyed:Decoration:EnableObservability` | bool | `true` | Decorator runtime flag. |
| `Alloyed:Decoration:EnableCorrelation` | bool | `true` | Decorator runtime flag. |
| `Alloyed:Decoration:EnableTransparency` | bool | `false` | Watch/transparency mode flag (next wave). |
| `Alloyed:Mocking:Enabled` | bool | `false` | Enables mock mode controls. |
| `Alloyed:Mocking:Mode` | `InMemory\|Moq\|Custom` | `InMemory` | Throws actionable error on invalid value. |

## Example `config/appsettings.yml`

```yaml
Alloyed:
  Runtime:
    FailOnSeverity: Warning
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
```
