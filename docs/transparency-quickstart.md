# Script Transparency Quickstart

Use this flow to run scripts with decorators, without renaming commands:

```powershell
Import-Module ./src/powershell/Alloyed.DevOps.Multitool.psd1 -Force
Enable-AlloyedTransparencyMode
./your-script.ps1
Disable-AlloyedTransparencyMode
```

Optional runtime tuning:

```powershell
$env:ALLOYED_CONSOLE_OUTPUT_MODE = "Rich"
$env:ALLOYED_TRANSPARENCY_VERBOSE = "true"
$env:ALLOYED_RUNTIME_MAX_RETRIES = "2"
$env:ALLOYED_RUNTIME_RETRY_DELAY_SEC = "2"
$env:ALLOYED_RUNTIME_EXPONENTIAL_BACKOFF = "true"
```
