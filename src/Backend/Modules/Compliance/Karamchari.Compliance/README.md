# Karamchari.Compliance

Compliance and governance: data retention policies and actions, legal holds with scopes, regulatory registers and rules, policy violations, compliance scoring with history and snapshots, and audit packages.

## Domain ownership
Cross-domain compliance posture. Domain-local compliance artifacts (e.g. TimeAttendance statutory registers, Payroll compliance filings) stay in their owning modules; this module aggregates and scores.

## Events published
Contract types live in `Karamchari.Compliance.Contracts`.

## Events consumed
| Event | Consumer |
|---|---|
| `PayrollRunCompletedIntegrationEvent` | `Consumers/PayrollComplianceConsumer.cs` |
| `TimesheetApprovedIntegrationEvent` | `Consumers/ComplianceEventConsumer.cs` |

## Database tables
Source of truth: `Persistence/ComplianceDbContext.cs` and `Migrations/`.

- `RetentionPolicy`, `RetentionAction`
- `LegalHold`, `LegalHoldScope`
- `RegulatoryRegister`, `RegulatoryRule`
- `PolicyViolation`
- `ComplianceScore`, `ComplianceScoreHistory`, `ComplianceSnapshot`
- `AuditPackage`

## Project dependencies
- `Karamchari.Core`
- `Karamchari.Core.Contracts`
- `Karamchari.Compliance.Contracts`

## Wiring
Self-registered via `DependencyInjection/ComplianceServiceCollectionExtensions.cs`, called from the API host.

## Testing
No dedicated test project yet. Compliance flows are exercised through `tests/Backend/Karamchari.Api.UnitTests` and the cross-cutting suites. Full sweep:

```powershell
.\run-all-tests.ps1
```
