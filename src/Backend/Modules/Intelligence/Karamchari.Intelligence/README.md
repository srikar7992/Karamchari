# Karamchari.Intelligence

Workforce intelligence scoring engine: burnout, attrition, workforce health, manager effectiveness, talent risk, workload fairness, absence contagion, dependency and schedule-quality scores, plus signals, executive insights, strategic scenarios, recommendations, forecasts, feature snapshots, outcome labels, causal chains, and score calculation audits.

Domain documentation: [docs/domains/Intelligence.md](../../../../../docs/domains/Intelligence.md)

## Domain ownership
Current-state, event-driven workforce scoring and recommendations. Forward-looking planning belongs to Forecasting; cross-domain decision orchestration belongs to PlatformIntelligence.

## Events published
Contract types live in `Karamchari.Intelligence.Contracts`. `Services/DriftDetectionWorker.cs` publishes drift events.

## Events consumed
`Consumers/WorkforceIntelligenceConsumer.cs`:
`TimesheetApprovedIntegrationEvent`, `LeaveCancelledIntegrationEvent`, `EmployeeOnboardedIntegrationEvent`, `EmployeeTerminatedIntegrationEvent`, `ShiftSwapApprovedIntegrationEvent`, `OvertimeRejectedIntegrationEvent`

## Database tables
Source of truth: `Persistence/IntelligenceDbContext.cs` and `Migrations/`. 28 sets, including:

`IntelligenceSignal`, `MetricDefinition`, `OrganizationalHealthSignal`, `WorkforceRiskSignal`, `ExecutiveInsight`, `StrategicWorkforceScenario`, `WorkforceSignalRecord`, `WorkforceBurnoutScore`, `WorkforceAttritionScore`, `WorkforceHealthScore`, `ManagerEffectivenessScore`, `WorkforceRecommendation`, `WorkforceScoreSnapshot`, `WorkforceForecast`, `TalentRiskScore`, `WorkloadFairnessScore`, `AbsenceContagionScore`, `WorkforceFeatureSnapshot`, `WorkforceOutcomeLabel`, `ScoreCalculationAudit`, `InterventionOutcome`, `WorkforceHotspot`, `EmployeeDependencyScore`, `EmployeeScheduleQuality`, `EmployeeSegmentProfile`, `WorkforceEmployeeScope`, `SiteCoverageFragility`, `EmployeeCausalChain`

## Project dependencies
- `Karamchari.Core`
- `Karamchari.Core.Contracts`
- `Karamchari.Intelligence.Contracts`

## Wiring
Self-registered via `DependencyInjection/IntelligenceServiceCollectionExtensions.cs`, called from the API host.

## Testing
```powershell
dotnet test tests/Backend/Karamchari.Intelligence.Tests/Karamchari.Intelligence.Tests.csproj
```
