# Karamchari.Performance

Performance management: goal cycles and goals, OKR cycles and objectives, KPI definitions/results/snapshots, review templates/cycles/assignments/submissions, 360 feedback (requests, submissions, in-flow feedback), calibration sessions, promotion recommendations, skill taxonomy and employee skill profiles, career frameworks, growth plans, export jobs, and manager-facing projections (dashboard, review inbox, calibration board, promotion pipeline, talent heatmap, team goal summary, skill inventory).

Domain documentation: [docs/domains/Performance.md](../../../../../docs/domains/Performance.md)

## Domain ownership
Performance evaluation lifecycle. Compensation outcomes of reviews belong to Compensation; HR consumes performance snapshots into its projections.

## Events published
Defined in `Karamchari.Performance.Contracts` (`IntegrationEvents/PerformanceIntegrationEvents.cs`):

`ReviewCycleCompletedIntegrationEvent`, `EmployeeCalibrationFinalizedIntegrationEvent`, `PromotionApprovedIntegrationEvent`, `CompensationRecommendationApprovedIntegrationEvent`, `GoalCycleActivatedIntegrationEvent`, `GoalCycleLockedIntegrationEvent`, `EmployeePerformanceSnapshotMaterializedIntegrationEvent`, `ReviewAssignedIntegrationEvent`, `ReviewSubmittedIntegrationEvent`, `FeedbackRequestCreatedIntegrationEvent`, `GoalApprovalRequiredIntegrationEvent`

## Events consumed
| Event | Consumer |
|---|---|
| `EmployeeOnboardedIntegrationEvent` | `Consumers/EmployeeOnboardedPerformanceConsumer.cs` |

## Database tables
Source of truth: `Persistence/PerformanceDbContext.cs` and `Migrations/`. 29 sets, including:

`GoalCycle`, `Goal`, `OKRCycle`, `Objective`, `KPIDefinition`, `KPIResult`, `KPISnapshot`, `ReviewTemplate`, `ReviewCycle`, `ReviewAssignment`, `ReviewSubmission`, `FeedbackRequest`, `FeedbackSubmission`, `InFlowFeedback`, `CalibrationSession`, `PromotionRecommendation`, `SkillTaxonomy`, `EmployeeSkillProfile`, `CareerFramework`, `EmployeeGrowthPlan`, `ExportJob`, `PerformanceSnapshot`, plus projections (`ManagerDashboardProjection`, `ReviewTaskInboxItem`, `CalibrationBoardProjection`, `PromotionPipelineItem`, `TalentHeatmapEntry`, `TeamGoalSummary`, `EmployeeSkillInventoryItem`).

## Project dependencies
- `Karamchari.Core`
- `Karamchari.Core.Contracts`
- `Karamchari.Performance.Contracts`

## Wiring
Self-registered via `DependencyInjection/PerformanceServiceCollectionExtensions.cs`, called from the API host.

## Testing
No dedicated test project yet. Performance flows are exercised via `tests/Backend/Karamchari.Api.UnitTests` and cross-cutting suites. Full sweep:

```powershell
.\run-all-tests.ps1
```
