# Performance Domain

## Extracted Knowledge

| Required Item | Evidence-backed Content |
|---|---|
| Purpose | Manages goals, OKRs, KPIs, review templates/cycles/assignments/submissions, feedback, calibration, promotions, skills, career frameworks, growth plans, exports, snapshots, and read models. Evidence: `src/Backend/Karamchari.Performance/Persistence/PerformanceDbContext.cs:41`. |
| Business Objectives | UNKNOWN beyond performance management and talent operations exposed by endpoints/read models. |
| Core Concepts | Goal cycle, goal, OKR cycle, objective, KPI definition/result/snapshot, review, feedback, calibration, promotion, skill taxonomy/profile, career framework, growth plan, export job, manager dashboard, talent heatmap. |
| Aggregates / Entities | DbSets in `PerformanceDbContext`. Evidence: `src/Backend/Karamchari.Performance/Persistence/PerformanceDbContext.cs:41`. |
| Value Objects | Goal progress/revision, KPI thresholds/bands, review sections/questions/responses, calibration records, career levels/tracks/competencies. Evidence: `src/Backend/Karamchari.Performance/Domain`. |
| State Machines | Goal cycle/status, OKR cycle/status, KPI result status, review cycle/submission statuses, feedback statuses, calibration session status, promotion status, growth plan status, export job status. Evidence: `src/Backend/Karamchari.Performance/Domain/**/*.cs`. |
| Events | Goal, review, calibration, promotion, performance snapshot integration events. Evidence: `src/Backend/Karamchari.Performance.Contracts/IntegrationEvents/PerformanceIntegrationEvents.cs:11`. |
| Commands | Workspace/report endpoints and event consumers; full command model UNKNOWN. Evidence: `src/Backend/Karamchari.Api/BFF/Manager/ManagerEndpoints.cs`, `src/Backend/Karamchari.Api/BFF/HR/HRWorkspaceEndpoints.cs`, `src/Backend/Karamchari.Api/BFF/HR/ExportJobEndpoints.cs`. |
| Queries | Manager dashboard, review inbox, team goals, promotion pipeline, talent heatmap, executive summaries, HR review cycles/calibration/promotions, employee goals/reviews/skills. Evidence: API BFF endpoint files. |
| Business Rules / Invariants / Validation | Scoring strategies and promotion readiness engine interfaces exist; exact formulas and approvals UNKNOWN. Evidence: `src/Backend/Karamchari.Performance/Domain/Scoring`. |
| Calculation Rules | OKR/review scoring and KPI formula engine are represented by code/ADRs; exact production rule catalog UNKNOWN. |
| Ownership Rules | Employee/manager/HR surfaces imply role-specific ownership, but authoritative rules UNKNOWN. |
| Dependencies | HR employee onboarding, notifications, compensation recommendations. Evidence: `src/Backend/Karamchari.Performance/Consumers/EmployeeOnboardedPerformanceConsumer.cs`, `src/Backend/Karamchari.Notifications/Consumers/*.cs`. |
| External Integrations | UNKNOWN. |
| Examples | `GET /api/v1/manager/dashboard`, `GET /api/v1/hr/calibration`, `GET /api/v1/executive/talent/heatmap`, `POST /api/v1/hr/reports/{type}`. |
| Failure Scenarios | Export cancellation endpoint exists. Evidence: `src/Backend/Karamchari.Api/BFF/HR/ExportJobEndpoints.cs:28`. |
| Known Limitations | No dedicated Performance test project found. |
