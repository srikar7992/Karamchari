# Intelligence Semantics Report

## 1. Conflicting Taxonomies
- **Risk:** A "Critical Risk" status in `AttendanceAnomaly` has different operational severity than a "Critical Risk" status in `WorkforceReadinessLevel`. Grouping these visually under the same red icon creates semantic chaos.
- **Mitigation:** UI components must strictly delineate between *Operational Severity* (immediate action required) and *Strategic Risk* (long-term planning required).

## 2. Evidence Weighting Variability
- **Risk:** Different modules attach different weights to evidence. 
- **Mitigation:** The `SignalConfidence` calculation must be centralized. A single `ConfidenceEvaluationEngine` should determine how "Manager Input" vs. "System Metric" vs. "Verified Certificate" maps to a 0-100 scale.
