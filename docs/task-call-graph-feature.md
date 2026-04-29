# Task: Script Call Graph (Static + Runtime)

## Summary
Implement a feature to build and export a call graph for PowerShell scripts to improve transparency, debugging, and migration validation.

## Why
- Help users understand execution flow in legacy scripts.
- Make decorated/transparency behavior auditable.
- Provide artifact output for CI diagnostics and documentation.

## Scope
- Build static call graph from PowerShell AST.
- Collect runtime call edges during transparency/session execution.
- Merge static and runtime data into a single model.
- Export graph as `Mermaid` and `JSON` (optional `DOT`).

## Proposed Commands
- `Get-AlloyedCallGraph`
- `Export-AlloyedCallGraph -Format Mermaid|Json`

## Acceptance Criteria (DoD)
- Given a script with functions/pipelines, the graph contains nodes and edges for discovered calls.
- Runtime mode marks observed edges separately from static edges.
- Exported Mermaid diagram renders correctly in GitHub Markdown.
- Unit tests cover parser and merge logic.
- Integration test validates end-to-end graph generation from a sample script.
- Docs updated with usage examples and limitations.

## Out of Scope (for first iteration)
- Full control-flow analysis with exact branch/path execution.
- Cross-process distributed tracing.

## Notes
- Keep first version deterministic and lightweight.
- Prefer minimal public API surface and clear output contracts.
