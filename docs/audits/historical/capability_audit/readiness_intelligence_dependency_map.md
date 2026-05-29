# Readiness Intelligence Dependency Map

## Upstream Dependencies
- **Karamchari.Performance:** Provides performance review scores as partial inputs to the `WorkforceCapabilityAssessment`.
- **Karamchari.TimeAttendance:** Provides fatigue and operational pressure metrics that might block aggressive upskilling programs temporarily.
- **Karamchari.Recruitment:** Provides `WorkforceDemandSignal` indicating external skills shortages which trigger internal capability mapping queries.

## Downstream Dependencies
- **Karamchari.Recruitment:** Receives `InternalMobility` readiness alerts from `CapabilityGapDetected` to recommend internal transfers over external hiring.
- **Karamchari.HR:** Subscribes to `CareerTrackUpdated` to synchronize job titles and compensation bands when promotions are realized.

## Eventing Integration
- `EnterpriseEventEnvelope` will strictly wrap all `Capability` events, routing them through the `Core` outbox relay.
