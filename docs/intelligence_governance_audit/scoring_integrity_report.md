# Scoring Integrity & Metric Report

## 1. Unversioned Calculations
- **Risk:** `ReadinessScore` calculations currently lack a deterministic, versioned definition. If the calculation logic shifts, historical scores become incomparable, destroying organizational trust in executive metrics.
- **Mitigation:** Implement a `MetricDefinition` aggregate. Any change to a scoring formula requires a new `MetricVersion`. Historical scores must permanently link to their evaluated version.

## 2. Metric Ownership Conflicts
- **Risk:** Without clear ownership, the "Hiring Urgency" metric might be calculated by both the ATS and the Operational Forecasting engine, leading to conflicting executive dashboards.
- **Mitigation:** Establish strict module-to-metric ownership rules. Metrics are treated as APIs, with clearly defined producers and consumers.
