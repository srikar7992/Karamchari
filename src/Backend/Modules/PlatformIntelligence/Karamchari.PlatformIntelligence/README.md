# Karamchari.PlatformIntelligence

Cross-domain decision intelligence: platform decisions, workforce scenarios with simulation runs, workforce optimization, executive digests, workforce risks, and recommendations.

## Domain ownership
Decisions and simulations that span multiple domains. Single-domain scoring belongs to Intelligence; compliance scoring belongs to Compliance — this module reads both (direct project references, the only module allowed to).

## Events published
None. This module has no Contracts project and defines no integration events.

## Events consumed
None (no `Consumers/` folder); it queries Intelligence and Compliance directly.

## Database tables
Source of truth: `Persistence/PlatformIntelligenceDbContext.cs` and `Migrations/`.

- `PlatformDecision`
- `WorkforceScenario`
- `SimulationRun`
- `WorkforceOptimization`
- `ExecutiveDigest`
- `WorkforceRisk`
- `Recommendation`

## Project dependencies
- `Karamchari.Core`
- `Karamchari.Core.Contracts`
- `Karamchari.Intelligence` (direct module reference)
- `Karamchari.Compliance` (direct module reference)

## Wiring
Self-registered via `DependencyInjection/PlatformIntelligenceExtensions.cs`, called from the API host.

## Testing
No dedicated test project yet. Full sweep:

```powershell
.\run-all-tests.ps1
```
