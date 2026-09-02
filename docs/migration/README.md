# Migration Archive Notes

## What TAF was

**TAF** was an earlier automation framework of my own, proposed as a modernization path for a
PowerShell-heavy delivery estate. The proposal was not taken up, so the framework moved to my own
repository, and this toolkit is what it became — the same idea rebuilt around AST analysis and a
generated command catalog.

That is why the documents in this folder read like an enterprise migration plan: the migration was
the original brief.

## About these documents

This folder holds a selected subset of the migration planning documents, kept for historical
context. They record what was planned, not what the code does today — where the two disagree, the
code and the ADRs in `docs/` are authoritative.

Included:

- `03-phased-migration-plan.md`
- `04-architecture-vision.md`
- `05-mvp-scope.md`
- `08-iteration-backlog.md`

Rationale:

- keep the planning context close to the project,
- avoid dependence on external local paths,
- preserve historical planning evidence while implementation moves forward.
