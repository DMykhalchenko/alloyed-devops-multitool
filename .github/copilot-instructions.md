# Copilot Instructions

This repository is a .NET + PowerShell project for script transformation and module generation.

## Build and test commands
- `pwsh -NoProfile -File ./dev.ps1 -Stage fast -Restore`
- `pwsh -NoProfile -File ./dev.ps1 -Stage ci`
- `dotnet test tests/dotnet/Alloyed.DevOps.Multitool.Tests.Unit/Alloyed.DevOps.Multitool.Tests.Unit.csproj -c Debug`
- `dotnet test tests/dotnet/Alloyed.DevOps.Multitool.Tests.Integration/Alloyed.DevOps.Multitool.Tests.Integration.csproj -c Debug`

## Project conventions
- Keep changes minimal and targeted.
- Do not add new dependencies without explicit approval.
- Prefer deterministic behavior and stable test fixtures.
- Update tests when changing transformation behavior.
- Do not commit secrets, tokens, or machine-specific credentials.

## Key areas
- `src/dotnet/Alloyed.DevOps.Multitool.Core.*` contains core contracts and implementations.
- `src/dotnet/Alloyed.DevOps.Multitool.Host.PowerShell` contains transformation pipeline composition.
- `src/powershell/Alloyed.DevOps.Multitool.psm1` exposes module commands.
- `tests/dotnet` contains unit and integration tests.
- `tests/powershell/Smoke.Module.Tests.ps1` is end-to-end smoke coverage.
