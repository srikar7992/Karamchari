# Explainability Readiness Report

## 1. Traceability of Aggregated Metrics
- **Current State:** `IntelligenceSignal` tracks `LineageData`. `MetricDefinition` tracks the current version of the formula used.
- **Gap:** There is no distinct presentation layer interface (DTO) specifically designed to unpack `LineageData` for an executive user.
- **Required Action:** Introduce `ScoreExplanation` structures. The UI needs standardized schemas to render the "Why?" (e.g., contributing factors, penalizing factors, missing evidence).

## 2. Drift Detection Alerting
- **Current State:** Analytical projections have a `LastProcessedOccurredAtUtc` timestamp.
- **Gap:** Stale data silently persists in the read models until an update event arrives. If the outbox relay fails or a background job hangs, the executive dashboard continues to display stale metrics as current.
- **Required Action:** Implement a `DriftDetectionService` that actively compares `LastProcessedOccurredAtUtc` against the current time and forces a `StaleDataWarning` into the signal if the threshold is breached.
