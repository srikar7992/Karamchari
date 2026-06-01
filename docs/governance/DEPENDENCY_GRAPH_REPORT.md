# Dependency Graph Report
**Generated:** 2026-05-31

## Allowed Dependencies
- Api -> Core
- Api -> Contracts
- Worker -> Core
- Worker -> Contracts
- Infrastructure -> Core
- Tests -> Any
- Module Core -> Module Contracts

## Forbidden Dependencies
- Core -> Api (Violates layer boundary)
- Core -> Worker (Violates layer boundary)
- Module A Core -> Module B Core (Violates modular monolith boundary)
- Api -> Module internals (Should go through Contracts)

## Violations Found
| Source Project | Target Project | Violation Type |
|---|---|---|
| `Karamchari.DataMigration` | `Karamchari.HR` | Module Core -> Module Core |
| `Karamchari.DataMigration` | `Karamchari.TimeAttendance` | Module Core -> Module Core |
| `Karamchari.DataMigration` | `Karamchari.Payroll` | Module Core -> Module Core |
| `Karamchari.Billing` | `Karamchari.TimeAttendance.Contracts` | Allowed (Contracts) |
| `Karamchari.Forecasting` | `Karamchari.Billing.Contracts` | Allowed (Contracts) |
| `Karamchari.Forecasting` | `Karamchari.TimeAttendance.Contracts` | Allowed (Contracts) |

## Cleanup Plan
- [ ] Refactor `Karamchari.DataMigration` to use Contracts of HR, TimeAttendance, and Payroll.
- [ ] Verify if `Karamchari.Notifications` depends on other module cores (grep showed Performance.Contracts and Payroll.Contracts, which is fine).
