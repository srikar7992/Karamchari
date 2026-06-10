# Karamchari.Engagement

Employee engagement: pulse surveys, peer recognition with badges, company announcements, and polls.

## Domain ownership
Engagement and internal communication artifacts. Engagement-derived risk scoring (burnout, attrition) belongs to the Intelligence module.

## Events published
None. This module has no Contracts project and defines no integration events.

## Events consumed
None (no `Consumers/` folder).

## Database tables
Source of truth: `Persistence/EngagementDbContext.cs` and `Migrations/`.

- `PulseSurvey`
- `EmployeeRecognition`
- `RecognitionBadge`
- `Announcement`
- `Poll`

## Project dependencies
- `Karamchari.Core`
- `Karamchari.Core.Contracts`

## Wiring
Self-registered via `DependencyInjection/EngagementServiceCollectionExtensions.cs`, called from the API host.

## Testing
No dedicated test project yet. Full sweep:

```powershell
.\run-all-tests.ps1
```
