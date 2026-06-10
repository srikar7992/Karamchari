# Karamchari.HR

Core HR and organization: employee master, org structure (departments, designations, branches, legal entities, cost centers, business units, locations, organization units), position management (positions, assignments, budgets, vacancies), reporting relationships, org scenarios and snapshots, and a wide set of projections (org hierarchy/metrics, position occupancy, vacancy pipeline, span of control, workforce graph, compensation/performance projections, flight risk, promotion readiness).

Domain documentation: [docs/domains/HR.md](../../../../../docs/domains/HR.md)

## Domain ownership
Employee and organization system of record. Recruitment owns candidates until hire; Compensation owns comp decisions; Performance owns reviews — HR consumes their events into projections.

## Events published
HR publishes shared events defined in `Karamchari.Core.Contracts`, including:

- `EmployeeOnboardedIntegrationEvent` (`Services/EmployeeService.cs`)
- `VacancyOpenedIntegrationEvent`, `VacancyClosedIntegrationEvent` (`Projections/VacancyPipelineProjectionHandler.cs`)

Internal commands (`CreateDepartmentCommand`, `OnboardEmployeeCommand`, `UpdateEmployeeCommand`) live under `Contracts/` inside this project.

## Events consumed
| Event | Consumer |
|---|---|
| `TenantProvisionedIntegrationEvent` | `Consumers/TenantProvisionedConsumer.cs` |
| `CandidateHiredIntegrationEvent` | `Consumers/CreateEmployeeOnCandidateHiredConsumer.cs` |
| `EmployeeCompensationRevisedIntegrationEvent` | `Consumers/IntelligenceProjectionConsumers.cs` |
| `EmployeePerformanceSnapshotMaterializedIntegrationEvent` | `Consumers/IntelligenceProjectionConsumers.cs` |
| `SkillValidatedIntegrationEvent` | `Consumers/IntelligenceProjectionConsumers.cs` |
| `DepartmentCreated` | `Messaging/Consumers/DepartmentCreatedConsumer.cs` |

## Database tables
Source of truth: `Persistence/HRDbContext.cs` and `Migrations/`. 34 sets, including:

`Employee`, `EmployeeRelationship`, `Department`, `Designation`, `Branch`, `LegalEntity`, `CostCenter`, `BusinessUnit`, `Location`, `OrganizationUnit`, `Position`, `PositionAssignment`, `PositionBudget`, `PositionVacancy`, `ReportingRelationship`, `FutureOrgScenario`, `ScenarioChange`, `OrganizationSnapshot`, `ProjectionVersionRegistry`, plus projections (`OrganizationHierarchyProjection`, `OrganizationMetricsProjection`, `PositionOccupancyProjection`, `PositionOccupantProjection`, `VacancyPipelineProjection`, `SpanOfControlProjection`, `ManagerDirectReportProjection`, `WorkforceGraphNode`, `WorkforceGraphEdge`, `EmployeeCompensationProjection`, `EmployeePerformanceProjection`, `EmployeeFlightRiskPrediction`, `FlightRiskDriverProjection`, `EmployeePromotionReadiness`, `PromotionReadinessDriverProjection`).

## Project dependencies
- `Karamchari.Core`
- `Karamchari.Core.Contracts`
- `Karamchari.Recruitment.Contracts`
- `Karamchari.Compensation.Contracts`
- `Karamchari.Performance.Contracts`

## Wiring
Self-registered via `DependencyInjection/HRServiceCollectionExtensions.cs`, called from the API host.

## Testing
```powershell
dotnet test tests/Backend/Karamchari.HR.Tests/Karamchari.HR.Tests.csproj
```
