# Organizational Governance Report

## 1. Metric Ownership & Definitions
- **Current State:** Core modules (HR, ATS, Capabilities) all calculate their own operational statuses.
- **Risk:** As cross-domain intelligence expands, ATS might define "Hiring Urgency" differently than Workforce Planning. 
- **Action:** Enforce canonical Metric Definitions stored in the `IntelligenceDbContext`. All strategic signals must map to a `MetricDefinition.Id`.

## 2. Intelligence Safety & Overrides
- **Risk:** A faulty analytical model scores a key employee as a "Flight Risk," causing a self-fulfilling prophecy of organizational panic.
- **Action:** Establish `HumanReview` boundaries. Executives and HR partners must have the ability to override, dispute, or explicitly ignore an intelligence signal, and that override action itself must be audited.
