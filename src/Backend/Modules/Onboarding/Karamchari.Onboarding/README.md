# Karamchari.Onboarding

Employee onboarding and offboarding: onboarding cases, tasks, documents, and reusable templates with template tasks.

## Domain ownership
Onboarding/offboarding process state. The employee record itself is created and owned by the HR module, which this project references directly.

## Events published
Defined in `Karamchari.Onboarding.Contracts` (`IntegrationEvents/OnboardingIntegrationEvents.cs`):

- `OnboardingCaseCreatedIntegrationEvent`
- `OnboardingCaseCompletedIntegrationEvent`
- `OnboardingDocumentSubmittedIntegrationEvent`
- `EmployeeOffboardedIntegrationEvent`

## Events consumed
None (no `Consumers/` folder).

## Database tables
Source of truth: `Persistence/OnboardingDbContext.cs` and `Migrations/`.

- `OnboardingCase`
- `OnboardingTask`
- `OnboardingDocument`
- `OnboardingTemplate`
- `TemplateTask`

## Project dependencies
- `Karamchari.Core`
- `Karamchari.Core.Contracts`
- `Karamchari.Onboarding.Contracts`
- `Karamchari.HR` (direct module reference)

## Wiring
Self-registered via `DependencyInjection/OnboardingServiceCollectionExtensions.cs`, called from the API host.

## Testing
No dedicated test project yet. Full sweep:

```powershell
.\run-all-tests.ps1
```
