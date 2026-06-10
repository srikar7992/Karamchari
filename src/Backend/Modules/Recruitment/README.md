# Recruitment Module

Hiring pipeline: job requisitions, candidates, applications with stage history, interviews and feedback, offers with approval workflow, recruitment workflow definitions/state/transitions, audit entries, and analytics read models.

Domain documentation: [docs/domains/Recruitment.md](../../../../docs/domains/Recruitment.md)

## Layout
Unlike the other modules (single project each), Recruitment is split into layered projects:

| Project | Role |
|---|---|
| `Karamchari.Recruitment.Core` | Domain model (requisitions, candidates, interviews, offers) |
| `Karamchari.Recruitment.Application` | Commands (`CreateRequisitionCommand`, `ApplyCandidateCommand`, `ScheduleInterviewCommand`, `CreateOfferCommand`, `HireCandidateCommand`, ...), handlers, `IRecruitmentDbContext` |
| `Karamchari.Recruitment.Infrastructure` | `Persistence/RecruitmentDbContext.cs`, `Migrations/` |
| `Karamchari.Recruitment.Api` | Endpoints (`RecruitmentEndpoints.cs`), DI wiring (`DependencyInjection/RecruitmentModule.cs`) |
| `Karamchari.Recruitment.Worker` | Background processing |
| `Karamchari.Recruitment.Contracts` | Integration events |

## Domain ownership
Candidate-to-hire lifecycle. On hire, HR consumes `CandidateHiredIntegrationEvent` and creates the employee record; the employee master belongs to HR.

## Events published
Defined in `Karamchari.Recruitment.Contracts` (`RecruitmentIntegrationEvents.cs`):

- `RequisitionPublishedIntegrationEvent`
- `CandidateAppliedIntegrationEvent`
- `InterviewScheduledIntegrationEvent`
- `InterviewCompletedIntegrationEvent`
- `OfferIssuedIntegrationEvent`
- `OfferAcceptedIntegrationEvent`
- `CandidateHiredIntegrationEvent` (published from `Application/EventHandlers/CandidateHiredHandler.cs`)

## Events consumed
None from other modules.

## Database tables
Source of truth: `Karamchari.Recruitment.Infrastructure/Persistence/RecruitmentDbContext.cs` and its `Migrations/`.

- `JobRequisition`
- `Candidate`
- `ApplicationHistory`
- `Interview`
- `InterviewFeedback`
- `Offer`
- `RecruitmentWorkflowDefinition`, `RecruitmentWorkflowState`, `RecruitmentWorkflowTransition`
- `RecruitmentAuditEntry`
- `AnalyticsReadModel`

## Project dependencies
Core/Application/Infrastructure reference `Karamchari.Core` (platform) and each other in strict order: Contracts ← Core ← Application ← Infrastructure ← Api/Worker.

## Testing
```powershell
dotnet test tests/Backend/Karamchari.Recruitment.Tests/Karamchari.Recruitment.Tests.csproj
```
