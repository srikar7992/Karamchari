# Executive Intelligence Architecture Report

## 1. Analytical Separation (OLTP vs OLAP)
- **Status:** Foundations established in Phase 1E.5. 
- **Design:** All strategic aggregates in Phase 1F will reside in the `Karamchari.Intelligence` context. These aggregates will consume denormalized projections fed by asynchronous domain events from HR, Payroll, ATS, and Attendance.
- **Aggregates:** 
  - `OrganizationalHealthSignal`: Multi-domain correlation.
  - `WorkforceRiskSignal`: High-exposure alerts.
  - `SuccessionReadinessSignal`: Leadership coverage.
  - `WorkforceForecast`: Predictive demand modeling.

## 2. Intelligence Lineage & Confidence
- **Rule:** Strategic signals cannot exist without a reference to their contributing `IntelligenceSignal` components.
- **Traceability:** Every `ExecutiveInsight` must allow a drill-down into the underlying operational signals (e.g., "Overtime Spikes" -> "AttendanceSession events").
