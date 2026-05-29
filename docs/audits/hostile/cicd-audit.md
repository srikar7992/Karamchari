# CI/CD Audit

## Files Audited

- `.github/workflows/ci.yml`
- `.github/workflows/deploy-api.yml`
- `.github/workflows/tenant-isolation-certification.yml`

## CI Findings

`ci.yml` includes:

- Restore locked dependency graph.
- Format verification.
- Build with warnings as errors.
- Tests with coverage.
- NuGet vulnerability audit.
- API publish artifact.
- Node install/audit/typecheck/build for portal.
- Mobile install/audit.
- TruffleHog verified-secret scan.

Local equivalent evidence:

- Backend build succeeded.
- Full backend test runner passed 10 projects.
- TRX and coverage XML artifacts were produced.

## CD Findings

`deploy-api.yml` is not a real production deployment pipeline:

- Azure login is commented out.
- Docker push is commented out.
- Bicep deployment is commented out.
- App Service deployment is commented out.
- Dev/staging/prod jobs echo validation success.
- No rollback, canary, blue-green, migration, smoke test, or artifact promotion is implemented.

## Verdict

CI is credible for backend test/build and some frontend/mobile governance. CD is not certified.
