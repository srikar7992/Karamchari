# Repository Inventory
**Generated:** 2026-05-31  
**Baseline Tag:** `sprint1-certified-baseline`

## Source Projects (src/Backend/)

### Platform
| Project | Physical Path | Root Namespace | Target |
|---|---|---|---|
| Karamchari.Core | `src/Backend/Platform/Karamchari.Core` | Karamchari.Core | net10.0 |
| Karamchari.Core.Contracts | `src/Backend/Platform/Karamchari.Core.Contracts` | Karamchari.Core.Contracts | net10.0 |
| Karamchari.Identity | `src/Backend/Platform/Karamchari.Identity` | Karamchari.Identity | net10.0 |
| Karamchari.Identity.Contracts | `src/Backend/Platform/Karamchari.Identity.Contracts` | Karamchari.Identity.Contracts | net10.0 |
| Karamchari.Identity.Infrastructure | `src/Backend/Platform/Karamchari.Identity.Infrastructure` | Karamchari.Identity.Infrastructure | net10.0 |

### Modules
| Project | Physical Path | Root Namespace | Bounded Context |
|---|---|---|---|
| Karamchari.Billing | `src/Backend/Modules/Billing/Karamchari.Billing` | Karamchari.Billing | Billing |
| Karamchari.Billing.Contracts | `src/Backend/Modules/Billing/Karamchari.Billing.Contracts` | Karamchari.Billing.Contracts | Billing |
| Karamchari.Capability | `src/Backend/Modules/Capability/Karamchari.Capability` | Karamchari.Capability | Capability |
| Karamchari.Capability.Contracts | `src/Backend/Modules/Capability/Karamchari.Capability.Contracts` | Karamchari.Capability.Contracts | Capability |
| Karamchari.Compensation | `src/Backend/Modules/Compensation/Karamchari.Compensation` | Karamchari.Compensation | Compensation |
| Karamchari.Compensation.Contracts | `src/Backend/Modules/Compensation/Karamchari.Compensation.Contracts` | Karamchari.Compensation.Contracts | Compensation |
| Karamchari.DataMigration | `src/Backend/Modules/DataMigration/Karamchari.DataMigration` | Karamchari.DataMigration | DataMigration |
| Karamchari.DataMigration.Contracts | `src/Backend/Modules/DataMigration/Karamchari.DataMigration.Contracts` | Karamchari.DataMigration.Contracts | DataMigration |
| Karamchari.FinancialOps | `src/Backend/Modules/FinancialOps/Karamchari.FinancialOps` | Karamchari.FinancialOps | FinancialOps |
| Karamchari.FinancialOps.Contracts | `src/Backend/Modules/FinancialOps/Karamchari.FinancialOps.Contracts` | Karamchari.FinancialOps.Contracts | FinancialOps |
| Karamchari.Forecasting | `src/Backend/Modules/Forecasting/Karamchari.Forecasting` | Karamchari.Forecasting | Forecasting |
| Karamchari.Governance | `src/Backend/Modules/Governance/Karamchari.Governance` | Karamchari.Governance | Governance |
| Karamchari.HR | `src/Backend/Modules/HR/Karamchari.HR` | Karamchari.HR | HR |
| Karamchari.Intelligence | `src/Backend/Modules/Intelligence/Karamchari.Intelligence` | Karamchari.Intelligence | Intelligence |
| Karamchari.Intelligence.Contracts | `src/Backend/Modules/Intelligence/Karamchari.Intelligence.Contracts` | Karamchari.Intelligence.Contracts | Intelligence |
| Karamchari.Notifications | `src/Backend/Modules/Notifications/Karamchari.Notifications` | Karamchari.Notifications | Notifications |
| Karamchari.Payroll | `src/Backend/Modules/Payroll/Karamchari.Payroll` | Karamchari.Payroll | Payroll |
| Karamchari.Payroll.Contracts | `src/Backend/Modules/Payroll/Karamchari.Payroll.Contracts` | Karamchari.Payroll.Contracts | Payroll |
| Karamchari.Performance | `src/Backend/Modules/Performance/Karamchari.Performance` | Karamchari.Performance | Performance |
| Karamchari.Performance.Contracts | `src/Backend/Modules/Performance/Karamchari.Performance.Contracts` | Karamchari.Performance.Contracts | Performance |
| Karamchari.PSA | `src/Backend/Modules/PSA/Karamchari.PSA` | Karamchari.PSA | PSA |
| Karamchari.Recruitment | `src/Backend/Modules/Recruitment/Karamchari.Recruitment` | Karamchari.Recruitment | Recruitment |
| Karamchari.Recruitment.Contracts | `src/Backend/Modules/Recruitment/Karamchari.Recruitment.Contracts` | Karamchari.Recruitment.Contracts | Recruitment |
| Karamchari.TimeAttendance | `src/Backend/Modules/TimeAttendance/Karamchari.TimeAttendance` | Karamchari.TimeAttendance | TimeAttendance |
| Karamchari.TimeAttendance.Contracts | `src/Backend/Modules/TimeAttendance/Karamchari.TimeAttendance.Contracts` | Karamchari.TimeAttendance.Contracts | TimeAttendance |
| Karamchari.Workflow | `src/Backend/Modules/Workflow/Karamchari.Workflow` | Karamchari.Workflow | Workflow |

### Hosts
| Project | Physical Path | Root Namespace |
|---|---|---|
| Karamchari.Api | `src/Backend/Hosts/Karamchari.Api` | Karamchari.Api |
| Karamchari.Worker | `src/Backend/Hosts/Karamchari.Worker` | Karamchari.Worker |

## Test Projects (tests/Backend/)
| Project | Physical Path | Type |
|---|---|---|
| Karamchari.Api.UnitTests | `tests/Backend/Karamchari.Api.UnitTests` | Unit |
| Karamchari.ArchitectureTests | `tests/Backend/Karamchari.ArchitectureTests` | Architecture |
| Karamchari.Core.IntegrationTests | `tests/Backend/Karamchari.Core.IntegrationTests` | Integration |
| Karamchari.Core.UnitTests | `tests/Backend/Karamchari.Core.UnitTests` | Unit |
| Karamchari.DataMigration.Tests | `tests/Backend/Karamchari.DataMigration.Tests` | Integrated |
| Karamchari.FinancialChaosTests | `tests/Backend/Karamchari.FinancialChaosTests` | Chaos |
| Karamchari.Identity.IntegrationTests | `tests/Backend/Karamchari.Identity.IntegrationTests` | Integration |
| Karamchari.PSA.Tests | `tests/Backend/Karamchari.PSA.Tests` | Integrated |
| Karamchari.Payroll.Tests | `tests/Backend/Karamchari.Payroll.Tests` | Integrated |
| Karamchari.TenantIsolationCertification | `tests/Backend/Karamchari.TenantIsolationCertification` | Certification |
| Karamchari.TimeAttendance.Tests | `tests/Backend/Karamchari.TimeAttendance.Tests` | Integrated |
