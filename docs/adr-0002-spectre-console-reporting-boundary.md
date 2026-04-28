# ADR-0002: Spectre.Console Reporting Boundary

Status: Accepted  
Date: 2026-04-28  
Related issue: `#17`

## Context

Before Bogus integration we want better local output readability, but we must not leak UI dependencies into core libraries.

## Decision

1. `Spectre.Console` is allowed only in `Host.PowerShell` layer.
2. `Core.*` projects remain UI-agnostic and dependency-stable.
3. Reporting goes through host abstraction (`IConsoleReporter`).
4. Plain text reporter is mandatory fallback and CI default.

## Consequences

Positive:
- improved local UX without touching core contracts,
- deterministic CI logs remain unchanged by default.

Tradeoff:
- temporary dual rendering paths (plain + rich).

## Implementation Phases

1. Introduce abstraction and plain implementation (no behavior change).
2. Add Spectre implementation behind host-only boundary.
3. Wire mode selection and non-interactive fallback.
4. Incrementally migrate host output paths.

