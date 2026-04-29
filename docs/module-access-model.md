# Module Access Model

This document defines how to grant controlled access to preview/stable module packages in GitHub Packages.

## Goal

- Allow selected users/teams to install the module.
- Keep publish rights restricted.
- Make CI/CD access explicit and auditable.

## Recommended Model

Use two organization teams:

- `alloyed-module-consumers`
- `alloyed-module-publishers`

Permissions:

- Consumers: package `Read`
- Publishers: package `Write` (or `Admin` only for release owners)

## GitHub Package Permission Strategy

1. Publish package from this repository.
2. Ensure package is linked to `Ligare-Method/alloyed-devops-multitool`.
3. In package settings, grant:
   - `Read` to `alloyed-module-consumers`
   - `Write` to `alloyed-module-publishers`
4. Remove direct user-level permissions unless exception is required.

## Token Policy

### Human users (local install)

Use PAT with minimum required scopes:

- `read:packages`
- `repo` if package/repository visibility requires it

### CI publish workflow

Prefer `GITHUB_TOKEN` from workflow with:

- `packages: write`
- `contents: read`

Do not use long-lived PAT in CI if not required.

## Onboarding Consumers

1. Add user to `alloyed-module-consumers`.
2. Provide package source + auth instructions.
3. Validate by running install from a clean shell/session.

## Offboarding

1. Remove user from team.
2. Revoke PATs if managed centrally.
3. Confirm package access is removed.

## Governance Notes

- Keep package permissions team-based, not person-based.
- Separate read and publish responsibilities.
- Review team membership periodically.
