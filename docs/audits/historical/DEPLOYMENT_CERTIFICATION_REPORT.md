# Deployment Certification Report

Verdict: FAILED.

## Evidence

`deploy-api.yml` contains:

- Docker build validation.
- Commented Azure login.
- Commented Docker push.
- Commented Bicep deployment.
- Commented App Service deployment.
- Dev/staging/prod jobs that echo validation success.

References: `.github/workflows/deploy-api.yml:16`, `.github/workflows/deploy-api.yml:26`, `.github/workflows/deploy-api.yml:37`, `.github/workflows/deploy-api.yml:50`, `.github/workflows/deploy-api.yml:58`, `.github/workflows/deploy-api.yml:66`, `.github/workflows/deploy-api.yml:76`.

## Requirement Result

| Requirement | Status |
|---|---|
| Infrastructure deployment | Not implemented in active workflow. |
| Application deployment | Not implemented in active workflow. |
| Database migration | Not implemented. |
| Smoke tests | Not implemented. |
| Rollback | Not implemented. |
| Blue-green | Not implemented. |
| Canary | Not implemented. |
| Environment promotion | Names exist; real promotion gates not proven. |
| Approval gates | GitHub environment name exists for prod; actual protection settings are not in repo. |
| Secrets validation | Not implemented. |
| Artifact versioning | CI publishes an API artifact; deploy workflow does not use a pushed immutable artifact. |
| Audit trail | GitHub Actions gives run history, but no deployment evidence is produced. |

## Why `deploy-api.yml` Was Not Replaced

The requested replacement requires real cloud resource names, credentials model, smoke targets, rollback target, migration policy, approval policy, and artifact registry. The repository does not contain enough evidence to generate a non-placeholder production deployment. Replacing it with invented Azure details would violate the non-negotiable rule against assumptions.

The correct next step is to capture actual deployment infrastructure and secrets contract first, then replace the workflow with executable steps.
