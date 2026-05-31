# Project Placement Report
**Generated:** 2026-05-31  
**Status:** VIOLATIONS FOUND — Relocation plan defined below

---

## Architecture Zones (Target)

```
src/Backend/
├── Platform/           ← Core kernel + Identity
├── Modules/            ← One subfolder per bounded context
│   └── <BC>/
│       ├── Karamchari.<BC>/           (domain + persistence)
│       └── Karamchari.<BC>.Contracts/ (contracts / events)
├── Hosts/              ← Deployable host applications
│   ├── Karamchari.Api/
│   └── Karamchari.Worker/
└── Karamchari.sln

tests/Backend/          ← ALL test projects, no exceptions
```

---

## Violations Found

### V-001: All source projects flat (no Zone hierarchy)
**Severity:** High  
**Count:** 32 projects  
**Current:** `src/Backend/<ProjectName>/`  
**Required:** `src/Backend/<Zone>/.../<ProjectName>/`

### V-002: Test projects inside src/Backend/
**Severity:** Critical  
**Count:** 3 projects  

| Project | Current Location | Required Location |
|---|---|---|
| Karamchari.PSA.Tests | `src/Backend/Karamchari.PSA.Tests/` | `tests/Backend/Karamchari.PSA.Tests/` |
| Karamchari.Payroll.Tests | `src/Backend/Karamchari.Payroll.Tests/` | `tests/Backend/Karamchari.Payroll.Tests/` |
| Karamchari.TimeAttendance.Tests | `src/Backend/Karamchari.TimeAttendance.Tests/` | `tests/Backend/Karamchari.TimeAttendance.Tests/` |

---

## Complete Relocation Plan

### Platform Zone

| Project | FROM | TO |
|---|---|---|
| Karamchari.Core | `src/Backend/Karamchari.Core/` | `src/Backend/Platform/Karamchari.Core/` |
| Karamchari.Core.Contracts | `src/Backend/Karamchari.Core.Contracts/` | `src/Backend/Platform/Karamchari.Core.Contracts/` |
| Karamchari.Identity | `src/Backend/Karamchari.Identity/` | `src/Backend/Platform/Karamchari.Identity/` |
| Karamchari.Identity.Contracts | `src/Backend/Karamchari.Identity.Contracts/` | `src/Backend/Platform/Karamchari.Identity.Contracts/` |
| Karamchari.Identity.Infrastructure | `src/Backend/Karamchari.Identity.Infrastructure/` | `src/Backend/Platform/Karamchari.Identity.Infrastructure/` |

### Hosts Zone

| Project | FROM | TO |
|---|---|---|
| Karamchari.Api | `src/Backend/Karamchari.Api/` | `src/Backend/Hosts/Karamchari.Api/` |
| Karamchari.Worker | `src/Backend/Karamchari.Worker/` | `src/Backend/Hosts/Karamchari.Worker/` |

### Modules Zone

| Project | FROM | TO |
|---|---|---|
| Karamchari.Billing | `src/Backend/Karamchari.Billing/` | `src/Backend/Modules/Billing/Karamchari.Billing/` |
| Karamchari.Billing.Contracts | `src/Backend/Karamchari.Billing.Contracts/` | `src/Backend/Modules/Billing/Karamchari.Billing.Contracts/` |
| Karamchari.Capability | `src/Backend/Karamchari.Capability/` | `src/Backend/Modules/Capability/Karamchari.Capability/` |
| Karamchari.Capability.Contracts | `src/Backend/Karamchari.Capability.Contracts/` | `src/Backend/Modules/Capability/Karamchari.Capability.Contracts/` |
| Karamchari.Compensation | `src/Backend/Karamchari.Compensation/` | `src/Backend/Modules/Compensation/Karamchari.Compensation/` |
| Karamchari.Compensation.Contracts | `src/Backend/Karamchari.Compensation.Contracts/` | `src/Backend/Modules/Compensation/Karamchari.Compensation.Contracts/` |
| Karamchari.DataMigration | `src/Backend/Karamchari.DataMigration/` | `src/Backend/Modules/DataMigration/Karamchari.DataMigration/` |
| Karamchari.DataMigration.Contracts | `src/Backend/Karamchari.DataMigration.Contracts/` | `src/Backend/Modules/DataMigration/Karamchari.DataMigration.Contracts/` |
| Karamchari.FinancialOps | `src/Backend/Karamchari.FinancialOps/` | `src/Backend/Modules/FinancialOps/Karamchari.FinancialOps/` |
| Karamchari.FinancialOps.Contracts | `src/Backend/Karamchari.FinancialOps.Contracts/` | `src/Backend/Modules/FinancialOps/Karamchari.FinancialOps.Contracts/` |
| Karamchari.Forecasting | `src/Backend/Karamchari.Forecasting/` | `src/Backend/Modules/Forecasting/Karamchari.Forecasting/` |
| Karamchari.Governance | `src/Backend/Karamchari.Governance/` | `src/Backend/Modules/Governance/Karamchari.Governance/` |
| Karamchari.HR | `src/Backend/Karamchari.HR/` | `src/Backend/Modules/HR/Karamchari.HR/` |
| Karamchari.Intelligence | `src/Backend/Karamchari.Intelligence/` | `src/Backend/Modules/Intelligence/Karamchari.Intelligence/` |
| Karamchari.Intelligence.Contracts | `src/Backend/Karamchari.Intelligence.Contracts/` | `src/Backend/Modules/Intelligence/Karamchari.Intelligence.Contracts/` |
| Karamchari.Notifications | `src/Backend/Karamchari.Notifications/` | `src/Backend/Modules/Notifications/Karamchari.Notifications/` |
| Karamchari.Payroll | `src/Backend/Karamchari.Payroll/` | `src/Backend/Modules/Payroll/Karamchari.Payroll/` |
| Karamchari.Payroll.Contracts | `src/Backend/Karamchari.Payroll.Contracts/` | `src/Backend/Modules/Payroll/Karamchari.Payroll.Contracts/` |
| Karamchari.Performance | `src/Backend/Karamchari.Performance/` | `src/Backend/Modules/Performance/Karamchari.Performance/` |
| Karamchari.Performance.Contracts | `src/Backend/Karamchari.Performance.Contracts/` | `src/Backend/Modules/Performance/Karamchari.Performance.Contracts/` |
| Karamchari.PSA | `src/Backend/Karamchari.PSA/` | `src/Backend/Modules/PSA/Karamchari.PSA/` |
| Karamchari.Recruitment | `src/Backend/Karamchari.Recruitment/` | `src/Backend/Modules/Recruitment/Karamchari.Recruitment/` |
| Karamchari.Recruitment.Contracts | `src/Backend/Karamchari.Recruitment.Contracts/` | `src/Backend/Modules/Recruitment/Karamchari.Recruitment.Contracts/` |
| Karamchari.TimeAttendance | `src/Backend/Karamchari.TimeAttendance/` | `src/Backend/Modules/TimeAttendance/Karamchari.TimeAttendance/` |
| Karamchari.TimeAttendance.Contracts | `src/Backend/Karamchari.TimeAttendance.Contracts/` | `src/Backend/Modules/TimeAttendance/Karamchari.TimeAttendance.Contracts/` |
| Karamchari.Workflow | `src/Backend/Karamchari.Workflow/` | `src/Backend/Modules/Workflow/Karamchari.Workflow/` |

### Tests Zone (misplaced → correct)

| Project | FROM | TO |
|---|---|---|
| Karamchari.PSA.Tests | `src/Backend/Karamchari.PSA.Tests/` | `tests/Backend/Karamchari.PSA.Tests/` |
| Karamchari.Payroll.Tests | `src/Backend/Karamchari.Payroll.Tests/` | `tests/Backend/Karamchari.Payroll.Tests/` |
| Karamchari.TimeAttendance.Tests | `src/Backend/Karamchari.TimeAttendance.Tests/` | `tests/Backend/Karamchari.TimeAttendance.Tests/` |

---

## Files Requiring Updates After Relocation

| File | Update Required |
|---|---|
| `src/Backend/Karamchari.sln` | All project paths |
| All `*.csproj` in `src/Backend/` | ProjectReference relative paths |
| All `*.csproj` in `tests/Backend/` | ProjectReference relative paths to src/Backend/ |
| `docker-compose.yml` | Dockerfile context paths for Api and Worker |
