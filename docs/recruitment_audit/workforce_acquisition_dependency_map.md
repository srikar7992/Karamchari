# Workforce Acquisition Dependency Map

## Upstream Dependencies
- **Karamchari.HR:** Provides structural metadata (Departments, Roles, Reporting Lines) required for `JobRequisition` approval routing.
- **Karamchari.Performance / Attendance:** Provides `WorkforceDemandSignal`s (e.g., critical skill shortages, high overtime areas prompting hiring necessity).

## Downstream Dependencies
- **Karamchari.HR:** Consumes `HiringDecisionCompleted` to initiate `Employee` onboarding.
- **Karamchari.Identity:** Requires notification to provision initial accounts upon successful onboarding pipeline start.

## Bounded Context Integrity
- `Karamchari.Recruitment` will remain completely isolated. It will not share databases with HR or Payroll. All cross-module data exchange will utilize the enterprise event outbox and `EnterpriseEventEnvelope`.
