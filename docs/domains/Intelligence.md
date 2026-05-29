# Intelligence Domain

## Extracted Knowledge

| Required Item | Evidence-backed Content |
|---|---|
| Purpose | Stores intelligence signals, metric definitions, organizational health signals, workforce risk signals, executive insights, and strategic workforce scenarios. Evidence: `src/Backend/Karamchari.Intelligence/Persistence/IntelligenceDbContext.cs:26`. |
| Business Objectives | UNKNOWN beyond strategy health evaluation, risk detection, insight acknowledgement, and scenario simulation exposed by APIs. |
| Core Concepts | Intelligence signal, metric definition, organizational health, workforce risk, executive insight, strategic workforce scenario, confidence, explanation evidence. |
| Aggregates / Entities | DbSets in `IntelligenceDbContext`. |
| Value Objects | `SignalConfidence`, `ScoreExplanation`. Evidence: `src/Backend/Karamchari.Intelligence/Domain/Primitives/SignalConfidence.cs`, `src/Backend/Karamchari.Intelligence/Domain/Signals/ScoreExplanation.cs`. |
| State Machines | `EvidenceType`, `RiskCategory`, `ConfidenceLevel`. Evidence: `src/Backend/Karamchari.Intelligence/Domain/Signals/ScoreExplanation.cs`, `src/Backend/Karamchari.Intelligence/Domain/Signals/WorkforceRiskSignal.cs`, `src/Backend/Karamchari.Intelligence/Domain/Primitives/SignalConfidence.cs`. |
| Events | `StaleIntelligenceAlertEvent`. Evidence: `src/Backend/Karamchari.Intelligence.Contracts/IntelligenceEvents.cs:6`. |
| Commands | Evaluate health, detect risks, acknowledge insight, run simulation. Evidence: `src/Backend/Karamchari.Api/BFF/Intelligence/StrategyEndpoints.cs:24`. |
| Queries | List health, risks, insights, scenarios. Evidence: `src/Backend/Karamchari.Api/BFF/Intelligence/StrategyEndpoints.cs:25`. |
| Business Rules / Invariants / Validation | Strategic intelligence engines and confidence evaluation exist; model semantics and thresholds UNKNOWN. Evidence: `src/Backend/Karamchari.Intelligence/Services/StrategicIntelligenceEngines.cs`, `src/Backend/Karamchari.Intelligence/Domain/Signals/ConfidenceEvaluationEngine.cs`. |
| Calculation Rules | Confidence, health, risk, and scenario calculations exist in services; exact formulas UNKNOWN without deeper extraction. |
| Ownership Rules | Executive/strategy read surface exists; decision authority UNKNOWN. |
| Dependencies | Core, API BFF, background drift detection. Evidence: `src/Backend/Karamchari.Intelligence/Services/DriftDetectionWorker.cs`. |
| External Integrations | UNKNOWN. |
| Examples | `POST /api/v1/strategy/health/evaluate`, `POST /api/v1/strategy/risk/detect`, `POST /api/v1/strategy/scenarios`. |
| Failure Scenarios | Stale intelligence alert and drift detection worker exist; operational response UNKNOWN. |
| Known Limitations | No dedicated Intelligence tests found. |
