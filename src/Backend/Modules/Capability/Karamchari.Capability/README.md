# Karamchari.Capability

Capability intelligence: skill taxonomy and graph, learning modules and enrollments, certifications, growth plans, role skill requirements, skill coverage/gap projections, career readiness, internal mobility marketplace, career progression edges, and succession projections (critical positions, successor candidates).

Domain documentation: [docs/domains/Capability.md](../../../../../docs/domains/Capability.md)

## Domain ownership
Skills, learning, and capability-derived intelligence. Performance reviews belong to the Performance module; succession planning aggregates here are projection-level (the Succession module owns succession plans as aggregates).

## Events published
Contract types live in `Karamchari.Capability.Contracts`. Shared skill events (`SkillAssignedIntegrationEvent`, `SkillExpiredIntegrationEvent`, `SkillValidatedIntegrationEvent`) are defined in `Karamchari.Core.Contracts`.

## Events consumed
| Event | Consumers |
|---|---|
| `SkillValidatedIntegrationEvent` | `SkillValidatedCoverageConsumer`, `SkillValidatedReadinessConsumer`, `SkillValidatedMobilityConsumer`, `SkillValidatedSuccessionConsumer`, `SkillValidatedGapConsumer` |
| `VacancyOpenedIntegrationEvent` | `VacancyOpenedMobilityConsumer` |
| `VacancyClosedIntegrationEvent` | `VacancyClosedMobilityConsumer` |

## Database tables
Source of truth: `Persistence/CapabilityDbContext.cs` and `Migrations/`. 25 sets, including:

`SkillDefinition`, `SkillCategory`, `SkillNode`, `SkillGraphRelationship`, `SkillEvidence`, `CapabilityDefinition`, `CapabilityProfile`, `TenantCapability`, `RoleSkillRequirement`, `LearningModule`, `LearningEnrollment`, `CertificationDefinition`, `CertificationAchievement`, `GrowthPlan`, `Opportunity`, `OpportunityParticipant`, `MobilityVacancy`, `CareerProgressionEdge`, `CriticalPosition`, and projections (`EmployeeCapabilityProjection`, `EmployeeSkillCoverageProjection`, `EmployeeSkillGapProjection`, `CareerReadinessProjection`, `InternalMobilityProjection`, `SuccessorCandidateProjection`).

## Project dependencies
- `Karamchari.Core`
- `Karamchari.Core.Contracts`
- `Karamchari.Capability.Contracts`

## Wiring
Self-registered via `DependencyInjection/CapabilityServiceCollectionExtensions.cs`, called from the API host.

## Testing
```powershell
dotnet test tests/Backend/Karamchari.Capability.Tests/Karamchari.Capability.Tests.csproj
```
