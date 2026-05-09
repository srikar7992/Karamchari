# Workforce Forecasting Readiness Report

## 1. Scenario Simulation Consistency
- **Goal:** Allow "What-If" analysis (e.g., "What if attrition increases by 10% in Engineering?").
- **Dependency:** Relies on `WorkforceForecast` read models being populated with accurate historical baseline data from `Attendance` and `Recruitment`.
- **Gap:** Currently, the system lacks a "Simulation Sandbox" state. Phase 1F must implement `StrategicWorkforceScenario` to isolate "What-If" projections from the operational "Current State."

## 2. Demand Signal Calibration
- **Requirement:** Demand forecasts must correlate `RecruitmentVelocity` with `SkillDecay`.
