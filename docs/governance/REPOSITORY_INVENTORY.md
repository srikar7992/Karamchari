# Repository Inventory
**Generated:** 2026-05-31  
**Baseline Tag:** `sprint1-certified-baseline`  
**Branch:** `chore/repo-cleanup-consolidation`

---

## Summary

| Category | Count |
|---|---|
| Source projects | 35 |
| Test projects | 11 |
| Correctly placed source projects | 0 (all flat in `src/Backend/`) |
| Misplaced test projects | 3 (in `src/Backend/` instead of `tests/Backend/`) |
| Bounded contexts | 20 |

---

## Platform Projects

| Project | Physical Path | Assembly | Root Namespace | Layer | References |
|---|---|---|---|---|---|
| Karamchari.Core | `src/Backend/Karamchari.Core` | Karamchari.Core | Karamchari.Core | Platform/Kernel | _(none)_ |
| Karamchari.Core.Contracts | `src/Backend/Karamchari.Core.Contracts` | Karamchari.Core.Contracts | Karamchari.Core.Contracts | Platform/Contracts | _(none)_ |
| Karamchari.Identity | `src/Backend/Karamchari.Identity` | Karamchari.Identity | Karamchari.Identity | Platform/Domain | Core, Identity.Contracts |
| Karamchari.Identity.Contracts | `src/Backend/Karamchari.Identity.Contracts` | Karamchari.Identity.Contracts | Karamchari.Identity.Contracts | Platform/Contracts | _(none)_ |
| Karamchari.Identity.Infrastructure | `src/Backend/Karamchari.Identity.Infrastructure` | Karamchari.Identity.Infrastructure | Karamchari.Identity.Infrastructure | Platform/Infrastructure | Identity |

---

## Host Projects

| Project | Physical Path | Assembly | Root Namespace | Layer | References |
|---|---|---|---|---|---|
| Karamchari.Api | `src/Backend/Karamchari.Api` | Karamchari.Api | Karamchari.Api | Host | All 20 modules + Platform |
| Karamchari.Worker | `src/Backend/Karamchari.Worker` | Karamchari.Worker | Karamchari.Worker | Host | Core, Core.Contracts + 15 modules |

---

## Module Projects

| Project | Physical Path | Bounded Context | Layer | References |
|---|---|---|---|---|
| Karamchari.Billing | `src/Backend/Karamchari.Billing` | Billing | Core | Core, Billing.Contracts, TimeAttendance.Contracts |
| Karamchari.Billing.Contracts | `src/Backend/Karamchari.Billing.Contracts` | Billing | Contracts | _(none)_ |
| Karamchari.Capability | `src/Backend/Karamchari.Capability` | Capability | Core | Core, Capability.Contracts |
| Karamchari.Capability.Contracts | `src/Backend/Karamchari.Capability.Contracts` | Capability | Contracts | Core |
| Karamchari.Compensation | `src/Backend/Karamchari.Compensation` | Compensation | Core | Core, Core.Contracts, Compensation.Contracts |
| Karamchari.Compensation.Contracts | `src/Backend/Karamchari.Compensation.Contracts` | Compensation | Contracts | _(none)_ |
| Karamchari.DataMigration | `src/Backend/Karamchari.DataMigration` | DataMigration | Core | Core, Core.Contracts, DataMigration.Contracts, HR, TimeAttendance, Payroll |
| Karamchari.DataMigration.Contracts | `src/Backend/Karamchari.DataMigration.Contracts` | DataMigration | Contracts | Core.Contracts |
| Karamchari.FinancialOps | `src/Backend/Karamchari.FinancialOps` | FinancialOps | Core | Core, Core.Contracts, FinancialOps.Contracts |
| Karamchari.FinancialOps.Contracts | `src/Backend/Karamchari.FinancialOps.Contracts` | FinancialOps | Contracts | _(none)_ |
| Karamchari.Forecasting | `src/Backend/Karamchari.Forecasting` | Forecasting | Core | Core, Billing.Contracts, TimeAttendance.Contracts |
| Karamchari.Governance | `src/Backend/Karamchari.Governance` | Governance | Core | Core |
| Karamchari.HR | `src/Backend/Karamchari.HR` | HR | Core | Core, Core.Contracts |
| Karamchari.Intelligence | `src/Backend/Karamchari.Intelligence` | Intelligence | Core | Core, Intelligence.Contracts |
| Karamchari.Intelligence.Contracts | `src/Backend/Karamchari.Intelligence.Contracts` | Intelligence | Contracts | Core |
| Karamchari.Notifications | `src/Backend/Karamchari.Notifications` | Notifications | Core | Core, Core.Contracts, Performance.Contracts, Payroll.Contracts |
| Karamchari.Payroll | `src/Backend/Karamchari.Payroll` | Payroll | Core | Core, Core.Contracts, Payroll.Contracts |
| Karamchari.Payroll.Contracts | `src/Backend/Karamchari.Payroll.Contracts` | Payroll | Contracts | _(none)_ |
| Karamchari.Performance | `src/Backend/Karamchari.Performance` | Performance | Core | Core, Core.Contracts, Performance.Contracts |
| Karamchari.Performance.Contracts | `src/Backend/Karamchari.Performance.Contracts` | Performance | Contracts | _(none)_ |
| Karamchari.PSA | `src/Backend/Karamchari.PSA` | PSA | Core | Core, Core.Contracts |
| Karamchari.Recruitment | `src/Backend/Karamchari.Recruitment` | Recruitment | Core | Core, Recruitment.Contracts |
| Karamchari.Recruitment.Contracts | `src/Backend/Karamchari.Recruitment.Contracts` | Recruitment | Contracts | Core |
| Karamchari.TimeAttendance | `src/Backend/Karamchari.TimeAttendance` | TimeAttendance | Core | Core, Core.Contracts, Billing.Contracts, TimeAttendance.Contracts |
| Karamchari.TimeAttendance.Contracts | `src/Backend/Karamchari.TimeAttendance.Contracts` | TimeAttendance | Contracts | _(none)_ |
| Karamchari.Workflow | `src/Backend/Karamchari.Workflow` | Workflow | Core | Core, Core.Contracts |

---

## Test Projects — Correctly Placed (`tests/Backend/`)

| Project | Physical Path | Type | Tests Subject |
|---|---|---|---|
| Karamchari.Api.UnitTests | `tests/Backend/Karamchari.Api.UnitTests` | Unit | Karamchari.Api |
| Karamchari.ArchitectureTests | `tests/Backend/Karamchari.ArchitectureTests` | Architecture | All modules |
| Karamchari.Core.IntegrationTests | `tests/Backend/Karamchari.Core.IntegrationTests` | Integration | Karamchari.Core |
| Karamchari.Core.UnitTests | `tests/Backend/Karamchari.Core.UnitTests` | Unit | Karamchari.Core |
| Karamchari.DataMigration.Tests | `tests/Backend/Karamchari.DataMigration.Tests` | Unit+Integration+Certification | DataMigration |
| Karamchari.FinancialChaosTests | `tests/Backend/Karamchari.FinancialChaosTests` | Chaos | FinancialOps |
| Karamchari.Identity.IntegrationTests | `tests/Backend/Karamchari.Identity.IntegrationTests` | Integration | Identity |
| Karamchari.TenantIsolationCertification | `tests/Backend/Karamchari.TenantIsolationCertification` | Certification | Core+Platform |

---

## Test Projects — MISPLACED (`src/Backend/` — incorrect!)

| Project | Current Path | Expected Path | Violation |
|---|---|---|---|
| Karamchari.PSA.Tests | `src/Backend/Karamchari.PSA.Tests` | `tests/Backend/Karamchari.PSA.Tests` | Test in src/ |
| Karamchari.Payroll.Tests | `src/Backend/Karamchari.Payroll.Tests` | `tests/Backend/Karamchari.Payroll.Tests` | Test in src/ |
| Karamchari.TimeAttendance.Tests | `src/Backend/Karamchari.TimeAttendance.Tests` | `tests/Backend/Karamchari.TimeAttendance.Tests` | Test in src/ |

---

## Infrastructure Files

| File | Location | Purpose |
|---|---|---|
| `Karamchari.sln` | `src/Backend/Karamchari.sln` | Solution file |
| `Directory.Build.props` | repo root | Global MSBuild properties |
| `Directory.Packages.props` | repo root | Central package management |
| `docker-compose.yml` | repo root | Local development stack |
| `Karamchari.Api/Dockerfile` | `src/Backend/Karamchari.Api/` | API container image |
| `Karamchari.Worker/Dockerfile` | `src/Backend/Karamchari.Worker/` | Worker container image |
