# Delivery Policy

Last updated: 2026-04-28

## Goal

Define supported delivery modes, required checks, and cost-aware execution rules.

## Supported Delivery Modes

## 1) GitHub Actions (default)

Use for:
- pull request validation,
- release workflow automation,
- reproducible public CI checks.

Policy:
- PR checks are the source of merge confidence.
- CI workflow must stay deterministic and self-contained.

## 2) Jenkins Podman remote (optional)

Use for:
- organization-specific or self-hosted scenarios,
- private infrastructure execution where GitHub-hosted minutes are constrained.

Policy:
- Jenkins is optional and must not be required for open-source contribution flow.
- GitHub Actions remains canonical for repository-level quality gates.

## Required Checks (Merge Baseline)

Minimum required checks before merge:

1. Build and tests:
- unit tests pass,
- integration tests pass,
- PowerShell smoke passes.

2. Generated artifacts:
- ports generation check passes (no drift from `ports.catalog.json`).

## Branch Protection / Ruleset Mapping

Recommended branch protection for `main`:
- require pull request before merge,
- require status checks to pass:
  - `Alloyed DevOps Multitool CI`
- block force-push and deletion for protected branches.

## Cost-Aware Execution Guidance

1. Prefer PR-triggered CI over push-triggered broad workflows.
2. Keep local pre-push checks enabled to fail early.
3. Run targeted stages locally (`unit`, `integration`, `smoke`) during active development.
4. Reserve optional heavy/container runs for PR-level validation or explicit release verification.

## Ownership Boundaries

- Repository maintainers own GitHub Actions policy and required checks.
- Teams using Jenkins own Jenkins pipeline definitions and environment-specific credentials/policies.
- Any Jenkins-specific behavior must not weaken GitHub Actions baseline quality requirements.

