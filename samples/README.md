# Samples

End-to-end runnable scripts that demonstrate the main Alloyed workflows. All scripts require the module to be imported first:

```powershell
Import-Module ./src/powershell/Alloyed.DevOps.Multitool.psd1
```

---

## `transform-demo.ps1` — AST transformation pipeline

Walks the full transformation lifecycle in three progressive steps:

1. **Catalog exploration** — lists every registered command-to-wrapper mapping grouped by verb.
2. **Dry-run validation** — runs `Test-AlloyedTransform` against `deploy-scenario.ps1` and prints diagnostics.
3. **Full transform** — writes an importable `.psm1`/`.psd1` module to disk (requires `-Execute`).

```powershell
# Steps 1 and 2 only (safe for demos — no files written)
./samples/transform-demo.ps1

# All three steps — also produces the output module
./samples/transform-demo.ps1 -Execute

# Override source script, module name, output directory, or console mode
./samples/transform-demo.ps1 -Execute -ScriptPath ./my-script.ps1 -ModuleName MyModule -OutputPath ./out -OutputMode Rich
```

| Parameter | Default | Description |
| --- | --- | --- |
| `-ScriptPath` | `./samples/deploy-scenario.ps1` | Source script to validate/transform |
| `-ModuleName` | `DeployScenarioModule` | Name of the generated module |
| `-OutputPath` | `./out` | Directory where the module is written |
| `-Execute` | off | When present, runs `New-AlloyedModuleTransform` after the dry-run |
| `-OutputMode` | `Rich` | Console rendering back-end (`Plain` or `Rich`) |

---

## `session-demo.ps1` — session mode and transparency

Walks the full session lifecycle: start → intercept native commands → switch transparency profiles → inspect state → stop.

Session mode replaces native PowerShell cmdlets (`Get-ChildItem`, `Test-Path`, `ConvertTo-Json`, etc.) with Alloyed proxy functions for the lifetime of the session. You call the native cmdlets by their normal names — session mode handles the interception automatically.

```powershell
./samples/session-demo.ps1

# Start with a different profile or console mode
./samples/session-demo.ps1 -Profile debug -OutputMode Rich
```

| Parameter | Default | Description |
| --- | --- | --- |
| `-OutputMode` | `Rich` | Console rendering back-end (`Plain` or `Rich`) |
| `-Profile` | `standard` | Starting transparency profile (`minimal`, `standard`, `debug`) |

Sections covered:

1. `Start-AlloyedSession` — initialises the host assembly, enables transparency, and activates session mode.
2. Native commands intercepted at `standard` profile — `Get-ChildItem`, `Test-Path`, `ConvertTo-Json`/`ConvertFrom-Json`, `Join-Path`/`Split-Path`.
3. Switch to `minimal` profile — reduced noise.
4. Switch to `debug` profile — full tag dump.
5. `Get-AlloyedSessionState` — combined state snapshot.
6. `Stop-AlloyedSession` — tears down interception and transparency.

---

## `deploy-scenario.ps1` — sample source script

A realistic multi-step deployment script used as the input to `transform-demo.ps1`. It references standard PowerShell cmdlets that have Alloyed catalog mappings (`Get-ChildItem`, `Test-Path`, `Set-Content`, `Invoke-WebRequest`, etc.) so the transformation demo has meaningful commands to replace.

Not intended to be run standalone — it is the *input* to the transformation pipeline, not a demo itself.
