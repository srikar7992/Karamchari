# Karamchari.Forecasting

Workforce planning and forecasting: demand/supply forecasts, capacity gaps, hiring gaps, coverage risks, scenario forecasts with results, skill expiry and substitution, retirement risk and policy, forecast accuracy tracking, and client payment profiles for revenue forecasting.

Domain documentation: [docs/domains/Forecasting.md](../../../../../docs/domains/Forecasting.md)

## Domain ownership
Forward-looking workforce and financial projections, built from event-sourced snapshots of other domains (employee index, attendance, attrition, approved leave). Current-state intelligence scoring belongs to the Intelligence module.

## Events published
None. This module has no Contracts project; it maintains read models from other modules' events.

## Events consumed
`Consumers/WorkforcePlanningConsumer.cs`:
`EmployeeOnboardedIntegrationEvent`, `EmployeeTerminatedIntegrationEvent`, `SkillAssignedIntegrationEvent`, `SkillExpiredIntegrationEvent`, `LeaveRequestApprovedIntegrationEvent`, `LeaveCancelledIntegrationEvent`, `TimesheetApprovedIntegrationEvent`, `HireOfferAcceptedIntegrationEvent`

`Consumers/ForecastUpdateConsumer.cs`:
`TimesheetApprovedIntegrationEvent`, `InvoiceIssuedIntegrationEvent`, `PaymentReceivedIntegrationEvent`

## Database tables
Source of truth: `Persistence/ForecastingDbContext.cs` and `Migrations/`. 22 sets, including:

`DemandForecast`, `SupplyForecast`, `CapacityGap`, `HiringGap`, `CoverageRisk`, `ForecastMetrics`, `ClientPaymentProfile`, `WorkforceEmployeeIndex`, `WorkforcePlanningProjection`, `WfpScenarioForecast`, `WfpScenarioResult`, `WfpSkillExpiry`, `WfpSkillSubstitution`, `WfpAttendanceSnapshot`, `WfpAttritionSnapshot`, `WfpApprovedLeaveRecord`, `WfpOvertimePolicy`, `WfpForecastAccuracy`, `WfpRecomputeEntry`, `WfpFutureHire`, `WfpContractExpiry`, `WfpRetirementRisk`, `WfpRetirementPolicy`

## Project dependencies
- `Karamchari.Core`
- `Karamchari.Core.Contracts`
- `Karamchari.Billing.Contracts`
- `Karamchari.TimeAttendance.Contracts`

## Wiring
Self-registered via `DependencyInjection/ForecastingServiceCollectionExtensions.cs`, called from the API host.

## Testing
```powershell
dotnet test tests/Backend/Karamchari.Forecasting.Tests/Karamchari.Forecasting.Tests.csproj
dotnet test tests/Backend/Karamchari.Forecasting.IntegrationTests/Karamchari.Forecasting.IntegrationTests.csproj
```
