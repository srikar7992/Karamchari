# Workforce Intelligence Governance Report

## 1. Signal Sprawl & Fragmentation
- **Risk:** Multiple modules (Capability, Recruitment, Performance) emit signals without a canonical definition. For example, `CapabilityGapDetected` in Learning and `WorkforceDemandSignal` in Recruitment could represent overlapping concepts with divergent schema.
- **Mitigation:** Introduce an `IntelligenceSignal` aggregate and a `SignalRegistry` to enforce ownership. Signals must use a standard `IntelligenceEnvelope` providing Confidence, Freshness, and Lineage metadata.

## 2. Confidence Semantics Missing
- **Risk:** All data points are currently treated with equal trust. An unverified "Skill Added by Manager" carries the same weight as "Certification Earned via Assessment".
- **Mitigation:** Implement `ConfidenceScore` scaling (0-100) bound to specific `EvidenceType` hierarchies. 
