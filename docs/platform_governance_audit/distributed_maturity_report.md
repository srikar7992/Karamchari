# Distributed Maturity Report

## 1. Consistency Boundaries & Outbox
- **Current State:** The MassTransit Outbox is globally configured.
- **Strength:** Excellent reliability for event publishing within the same EF transaction.
- **Risk:** Without an explicitly governed `Replay` capability and `PoisonMessage` quarantine, downstream failures (e.g. invalid payload schemas) currently cause the consumer to infinitely retry and eventually hit a dead-letter state without a clear operational recovery path.
- **Action:** Formalize `DeadLetterGovernance` and outbox replay dashboards.

## 2. Projection Reliability
- **Current State:** Projections use `LastProcessedOccurredAtUtc` for out-of-order protection. `DriftDetectionWorker` actively alerts on stale projections.
- **Gap:** Cannot easily "rebuild from scratch" if the projection schema evolves.
- **Action:** Establish a governed `RebuildProjection` workflow utilizing event sourcing concepts for safe historical replays.
