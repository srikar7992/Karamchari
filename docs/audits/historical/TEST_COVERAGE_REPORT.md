# Test Coverage Report

Verdict: FAILED.

## Evidence Found

Test projects/files found:

- `tests/Backend/Karamchari.Api.UnitTests`
- `tests/Backend/Karamchari.ArchitectureTests`
- `tests/Backend/Karamchari.Core.UnitTests`
- `tests/Backend/Karamchari.Core.IntegrationTests`
- `tests/Backend/Karamchari.FinancialChaosTests`
- `tests/Backend/Karamchari.Identity.IntegrationTests`
- `tests/Backend/Karamchari.TenantIsolationCertification`
- Source-colocated tests: `src/Backend/Karamchari.Payroll.Tests`, `src/Backend/Karamchari.TimeAttendance.Tests`, `src/Backend/Karamchari.PSA.Tests`

## Required Modules

| Module | Unit | Integration | Architecture | Boundary | Failure | Security | Mutation |
|---|---|---|---|---|---|---|---|
| Billing | UNKNOWN/insufficient | UNKNOWN | Covered only generically if architecture tests include it | UNKNOWN | UNKNOWN | UNKNOWN | Missing |
| Workflow | UNKNOWN/insufficient | UNKNOWN | Generic tenant architecture tests exist | Some workflow concurrency tests | Partial | UNKNOWN | Missing |
| Forecasting | UNKNOWN/insufficient | UNKNOWN | UNKNOWN | UNKNOWN | UNKNOWN | UNKNOWN | Missing |
| Compensation | UNKNOWN/insufficient | UNKNOWN | UNKNOWN | UNKNOWN | UNKNOWN | UNKNOWN | Missing |
| Recruitment | UNKNOWN/insufficient | UNKNOWN | UNKNOWN | UNKNOWN | UNKNOWN | UNKNOWN | Missing |
| Governance | UNKNOWN/insufficient | UNKNOWN | UNKNOWN | UNKNOWN | UNKNOWN | UNKNOWN | Missing |
| Capability | UNKNOWN/insufficient | UNKNOWN | UNKNOWN | UNKNOWN | UNKNOWN | UNKNOWN | Missing |
| Notifications | UNKNOWN/insufficient | UNKNOWN | UNKNOWN | UNKNOWN | UNKNOWN | UNKNOWN | Missing |
| FinancialOps | Some chaos/survivability tests | UNKNOWN | UNKNOWN | UNKNOWN | Partial | UNKNOWN | Missing |
| Performance | UNKNOWN/insufficient | UNKNOWN | UNKNOWN | UNKNOWN | UNKNOWN | UNKNOWN | Missing |

## Mutation Testing

No Stryker configuration was found. Mutation testing requirement is unmet.

## Verification Attempt

`dotnet test src/Backend/Karamchari.sln --no-restore -c Release --logger "trx;LogFileName=institutionalization-audit.trx"` failed in the sandbox before tests executed with MSBuild `SocketException (13): Permission denied` while creating MSBuild named-pipe nodes. The hung process was stopped. This is not a product test failure, but it means local verification was not completed in this environment.
