# Karamchari Asynchronous Message Runtime Catalog

This catalog documents the runtime message topology, queues, exchanges, and consumers registered under the MassTransit worker pool.

---

## 1. Registered Consumers and Event Routing

The following table documents the active message routes inside the MassTransit bus registration, configured in [WorkerServiceCollectionExtensions.cs](src/Backend/Karamchari.Worker/DependencyInjection/WorkerServiceCollectionExtensions.cs):

| Event / Message Type | Publisher Context | Target Exchange | Target Queue | Consumer / Saga Class | Dead Letter Queue (DLQ) |
| :--- | :--- | :--- | :--- | :--- | :--- |
| `TenantProvisioned` | Provisioning Service | `TenantProvisioned` | `TenantProvisioned` | `HR.Consumers.TenantProvisionedConsumer` | `TenantProvisioned_error` |
| `TenantProvisioned` | Provisioning Service | `TenantProvisioned` | `TenantProvisioned` | `Payroll.Consumers.TenantProvisionedConsumer` | `TenantProvisioned_error` |
| `TenantProvisioned` | Provisioning Service | `TenantProvisioned` | `TenantProvisioned` | `TimeAttendance.Consumers.TenantProvisionedConsumer` | `TenantProvisioned_error` |
| `EmployeeOnboarded` | HR Module | `EmployeeOnboarded` | `EmployeeOnboarded` | `Payroll.Consumers.EmployeeOnboardedConsumer` | `EmployeeOnboarded_error` |
| `CalculateAllEmployeePayCommand`| Payroll BFF | `CalculateAllEmployeePayCommand` | `CalculateAllEmployeePayCommand` | `Payroll.Consumers.CalculateAllEmployeePayCommandConsumer` | `CalculateAllEmployeePayCommand_error` |
| `PayrollBatch` | Payroll Module | `PayrollBatch` | `PayrollBatch` | `Payroll.Consumers.PayrollBatchConsumer` | `PayrollBatch_error` |
| `GeneratePayslip` | Payroll Module | `GeneratePayslip` | `GeneratePayslip` | `Payroll.Consumers.GeneratePayslipConsumer` | `GeneratePayslip_error` |
| `PayrollRunLocked` | Payroll Module | `PayrollRunLocked` | `PayrollRunLocked` | `Payroll.Consumers.PayrollRunLockedConsumer` | `PayrollRunLocked_error` |
| `LeaveRequestApproved`| Time & Attendance | `LeaveRequestApproved` | `LeaveRequestApproved` | `Payroll.Consumers.LeaveRequestApprovedConsumer` | `LeaveRequestApproved_error` |
| `TimesheetApproved` | Time & Attendance | `TimesheetApproved` | `TimesheetApproved` | `Payroll.Consumers.TimesheetApprovedConsumer` | `TimesheetApproved_error` |
| `TimesheetApproved` | Time & Attendance | `TimesheetApproved` | `TimesheetApproved` | `TimeAttendance.Consumers.TimesheetApprovedConsumer` | `TimesheetApproved_error` |
| `InitiateFnFCalculation`| HR / BFF | `InitiateFnFCalculation` | `InitiateFnFCalculation` | `Payroll.Consumers.InitiateFnFCalculationConsumer` | `InitiateFnFCalculation_error` |
| `DisburseFnF` | Payroll Module | `DisburseFnF` | `DisburseFnF` | `Payroll.Consumers.DisburseFnFConsumer` | `DisburseFnF_error` |
| `SalaryRevisionApprovedArrear`| Compensation / BFF | `SalaryRevisionApprovedArrear`| `SalaryRevisionApprovedArrear`| `Payroll.Consumers.SalaryRevisionApprovedArrearConsumer`| `SalaryRevisionApprovedArrear_error`|
| `ArrearApproved` | Compensation / BFF | `ArrearApproved` | `ArrearApproved` | `Payroll.Consumers.ArrearApprovedConsumer` | `ArrearApproved_error` |
| `InitiateDisbursement`| Payroll Module | `InitiateDisbursement` | `InitiateDisbursement` | `Payroll.Consumers.InitiateDisbursementConsumer` | `InitiateDisbursement_error` |
| `RetryDisbursement` | Payroll Module | `RetryDisbursement` | `RetryDisbursement` | `Payroll.Consumers.RetryDisbursementConsumer` | `RetryDisbursement_error` |
| `WorkflowCompletedPayroll`| Workflow Module | `WorkflowCompletedPayroll`| `WorkflowCompletedPayroll`| `Payroll.Consumers.WorkflowCompletedPayrollConsumer`| `WorkflowCompletedPayroll_error`|
| `TimesheetApprovedAnalytics`| Time & Attendance | `TimesheetApprovedAnalytics`| `TimesheetApprovedAnalytics`| `TimeAttendance.Consumers.TimesheetApprovedAnalyticsConsumer`| `TimesheetApprovedAnalytics_error`|
| `BillingAnalytics` | Time & Attendance | `BillingAnalytics` | `BillingAnalytics` | `TimeAttendance.Consumers.BillingAnalyticsConsumer` | `BillingAnalytics_error` |
| `WorkflowCompleted` | Workflow Module | `WorkflowCompleted` | `WorkflowCompleted` | `TimeAttendance.Consumers.WorkflowCompletedConsumer` | `WorkflowCompleted_error` |
| `WorkflowTraceability`| Workflow Module | `WorkflowTraceability` | `WorkflowTraceability` | `Workflow.Consumers.WorkflowTraceabilityConsumer` | `WorkflowTraceability_error` |
| `WorkflowOrchestrator`| Workflow Module | `WorkflowOrchestrator` | `WorkflowOrchestrator` | `Workflow.Consumers.WorkflowOrchestratorConsumer` | `WorkflowOrchestrator_error` |
| `BillableEntry` | Time & Attendance | `BillableEntry` | `BillableEntry` | `Billing.Consumers.BillableEntryConsumer` | `BillableEntry_error` |
| `CollectionCase` | Billing Module | `CollectionCase` | `CollectionCase` | `Billing.Consumers.CollectionCaseConsumer` | `CollectionCase_error` |
| `ForecastUpdate` | Billing Module | `ForecastUpdate` | `ForecastUpdate` | `Forecasting.Consumers.ForecastUpdateConsumer` | `ForecastUpdate_error` |
| `BillableRevenue` | Billing Module | `BillableRevenue` | `BillableRevenue` | `PSA.Consumers.BillableRevenueConsumer` | `BillableRevenue_error` |
| `ProfitCalculation` | Billing Module | `ProfitCalculation` | `ProfitCalculation` | `PSA.Consumers.ProfitCalculationConsumer` | `ProfitCalculation_error` |
| `ReviewAssigned` | Performance Module | `ReviewAssigned` | `ReviewAssigned` | `Notifications.Consumers.ReviewAssignedConsumer` | `ReviewAssigned_error` |
| `ReviewSubmitted` | Performance Module | `ReviewSubmitted` | `ReviewSubmitted` | `Notifications.Consumers.ReviewSubmittedConsumer` | `ReviewSubmitted_error` |
| `CalibrationFinalized`| Performance Module | `CalibrationFinalized` | `CalibrationFinalized` | `Notifications.Consumers.CalibrationFinalizedConsumer`| `CalibrationFinalized_error`|
| `PromotionApproved` | Compensation / BFF | `PromotionApproved` | `PromotionApproved` | `Notifications.Consumers.PromotionApprovedConsumer` | `PromotionApproved_error` |
| `GoalCycleActivated`| Performance Module | `GoalCycleActivated` | `GoalCycleActivated` | `Notifications.Consumers.GoalCycleActivatedConsumer` | `GoalCycleActivated_error` |
| `FeedbackRequestCreated`| Performance Module | `FeedbackRequestCreated`| `FeedbackRequestCreated`| `Notifications.Consumers.FeedbackRequestCreatedConsumer`| `FeedbackRequestCreated_error`|
| `GoalApprovalRequired`| Performance Module | `GoalApprovalRequired` | `GoalApprovalRequired` | `Notifications.Consumers.GoalApprovalRequiredConsumer` | `GoalApprovalRequired_error` |
| `PayrollNotification`| Payroll Module | `PayrollNotification` | `PayrollNotification` | `Notifications.Consumers.PayrollNotificationConsumer` | `PayrollNotification_error` |

---

## 2. MassTransit Sagas & State Machines

| Saga State Type | State Machine Class | Description / Purpose |
| :--- | :--- | :--- |
| `PayrollRunState` | `PayrollRunStateMachine` | Orchestrates salary runs, tax computations, and locks. |
| `FnFSettlementState` | `FnFSettlementStateMachine` | Tracks employee final dues clearance and exit payouts. |
| `DisbursementBatchState`| `DisbursementBatchStateMachine`| Manages batch banking uploads and retry handling loops. |
| `PayrollCorrectionState`| `PayrollCorrectionStateMachine`| Tracks salary corrections, overrides, and retro calculations. |

---

## 3. Resiliency Policies (Standard Event Handlers)

1.  **Transport Level Deduplication**: All event handlers inspect the `X-Content-Hash` (SHA-256) of the incoming message body. If already present in the local cache or DB outbox state trackers, processing is bypassed immediately.
2.  **Retry Backoff**: 3 immediate retries at 5-second intervals.
3.  **Tenant Scoping**: `TenantPublishFilter` and `TenantConsumeFilter` validate that the `X-Tenant-Id` header is matching the active tenant scope context. Unmatched messages trigger a `MalformedTenantMessageException` and are discarded to the DLQ to prevent cross-tenant operations.
