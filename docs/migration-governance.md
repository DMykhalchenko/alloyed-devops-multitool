# Migration Governance and Completion Policy

Last updated: 2026-04-28

## Goal

Provide explicit criteria to declare migration complete and define maintenance/triage expectations.

## Migration Completion Criteria

Migration is considered complete when all items below are true:

1. Functional coverage:
- planned system port waves are implemented according to ADR and catalog policy.

2. Quality baseline:
- unit, integration, and smoke suites pass in CI on protected branch flow.

3. Governance artifacts:
- alloy spec, architecture ADRs, contracts/versioning policy, and delivery policy are published and current.

4. Operational readiness:
- rollback path and incident response guidance are documented.

5. Backlog state:
- remaining open issues are either:
  - accepted future enhancements, or
  - explicitly deferred with rationale.

## Ownership Model

- Maintainers:
  - approve contract/architecture changes,
  - enforce required CI checks,
  - own release and deprecation decisions.

- Contributors:
  - follow contract/versioning and ADR policies,
  - provide test evidence and docs updates for behavior changes.

## Support Expectations

- Supported branch: `main`.
- Public support model: best-effort through issues and pull requests.
- Breaking changes require major version intent and deprecation notice.

## Backlog Triage Policy

Issue triage classes:
- `bug`: regressions or broken expected behavior.
- `enhancement`: additive features.
- `governance/docs`: policy or documentation upkeep.

Priority order:
1. correctness/regression fixes,
2. CI/release stability,
3. planned migration milestones,
4. optional UX improvements.

## Rollback Guidance

Rollback unit is commit/PR scope.

Rules:
1. keep changes small and reversible,
2. avoid cross-cutting unrelated edits in one PR,
3. if regression detected post-merge, revert latest offending commit first, then patch forward.

## Incident Response (Transformed Module)

When transformed output causes runtime issues:

1. Reproduce with minimal script fixture.
2. Run local baseline checks:
- `dev.ps1 -Stage unit`
- `dev.ps1 -Stage integration`
- `dev.ps1 -Stage smoke`
3. Validate catalog sync and generated artifacts.
4. If production impact is active:
- disable problematic migration path,
- roll back to previous known-good module artifact,
- open incident issue with reproduction and diagnostics.

## Declaration Process

To declare migration complete:

1. open a completion PR updating:
- `docs/migration-status-matrix.md`
- this governance document,
- any remaining milestone docs.
2. include checklist evidence for completion criteria.
3. require maintainer approval and passing required checks.

