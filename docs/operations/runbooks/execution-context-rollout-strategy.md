# Strategy: Execution Context System Rollout

## Overview
This document defines the 4-stage Canary Rollout strategy for the Execution Context Preservation System to ensure zero disruption to business-critical integration events.

## Rollout Phases

### Stage 1: Ephemeral (Internal Dev)
- **Scope**: Internal PR environments and local development.
- **Goal**: Verify generic filters don't break existing local workflows.
- **Success Criteria**: All local integration tests pass.

### Stage 2: Staging (100% Traffic)
- **Scope**: Staging environment.
- **Goal**: Execute the **Staging Certification Runbook**.
- **Observation Period**: 24 hours.
- **Gate**: Lead Engineer sign-off on Staging Evidence Package.

### Stage 3: Production Canary (1%)
- **Scope**: Target specific non-critical background workers or a single region.
- **Goal**: Observe real-world HMAC latency and signature validity across a small subset of events.
- **Gate**: `tenant_event_rejection_rate` must remain 0% for 4 hours.

### Stage 4: Production Incremental (10% -> 50% -> 100%)
- **Scope**: Entire platform.
- **Goal**: Full migration of all integration events to the Preservation system.
- **Increment Interval**: 12 hours per step.
- **Monitoring**: Close monitoring of the `TenantSignatureValidationFailed` alert.

## Rollback Procedure
If `tenant_event_rejection_rate` spikes above 1% for legitimate traffic:
1.  **Stop Rollout**: Freeze any further canary progression.
2.  **Toggle Fallback**: Deploy configuration change to set `Messaging:ValidationMode = AuditOnly` (if implemented) or revert `MassTransitExtensions` filters.
3.  **Emergency Rotation**: If failure is due to key mismatch, perform an emergency key rotation following the ADR-0016 procedures.

## Conclusion
The rollout is complete only when 100% of integration events carry valid, signed metadata and all dashboard indicators are green.
