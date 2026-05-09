// Phase 1A — Payroll Operational Maturity types
// Mirrors Karamchari.Api.BFF.Payroll.PayrollDtos

// ── FnF ─────────────────────────────────────────────────────────────────────

export type FnFExitType =
  | 'Resignation'
  | 'Termination'
  | 'Absconding'
  | 'Retirement'
  | 'ContractCompletion';

export type FnFStatus =
  | 'Draft'
  | 'PendingApproval'
  | 'Approved'
  | 'Disbursed'
  | 'OnHold'
  | 'Reopened'
  | 'Cancelled'
  | 'PostFnFCorrection';

export interface FnFLineItemDto {
  lineItemType: string;
  description: string;
  amount: number;
  isDeduction: boolean;
  signedAmount: number;
}

export interface FnFSettlementDto {
  id: string;
  employeeId: string;
  employeeName: string;
  exitType: FnFExitType;
  status: FnFStatus;
  lastWorkingDate: string;
  exitDate: string;
  lineItems: FnFLineItemDto[];
  grossPayable: number;
  totalDeductions: number;
  netPayable: number;
  createdAtUtc: string;
}

export interface InitiateFnFRequest {
  employeeId: string;
  employeeName: string;
  exitType: FnFExitType;
  lastWorkingDate: string;
  exitDate: string;
  remarks?: string;
}

// ── Arrears ─────────────────────────────────────────────────────────────────

export type ArrearStatus =
  | 'Pending'
  | 'PendingApproval'
  | 'Approved'
  | 'Processed'
  | 'Reversed'
  | 'Cancelled';

export type ArrearTriggerType =
  | 'SalaryRevision'
  | 'AttendanceCorrection'
  | 'ShiftCorrection'
  | 'OvertimeCorrection'
  | 'TaxRecalculation'
  | 'BackdatedPromotion'
  | 'ComplianceAdjustment'
  | 'Manual'
  | 'FnFReopen';

export interface ArrearPeriodDiffDto {
  periodName: string;
  previousGross: number;
  recomputedGross: number;
  previousNet: number;
  recomputedNet: number;
  previousTds: number;
  recomputedTds: number;
  grossDelta: number;
  netDelta: number;
  tdsDelta: number;
}

export interface ArrearCalculationDto {
  id: string;
  employeeId: string;
  employeeName: string;
  triggerType: ArrearTriggerType;
  triggerReference: string;
  status: ArrearStatus;
  fromPeriod: string;
  toPeriod: string;
  periodDiffs: ArrearPeriodDiffDto[];
  totalArrearAmount: number;
  totalTdsAdjustment: number;
  createdAtUtc: string;
}

export interface TriggerArrearRequest {
  employeeId: string;
  employeeName: string;
  triggerType: ArrearTriggerType;
  triggerReference: string;
  fromPeriod: string;
  toPeriod: string;
}

// ── Corrections ─────────────────────────────────────────────────────────────

export type CorrectionStatus =
  | 'Draft'
  | 'PendingApproval'
  | 'Approved'
  | 'RecalculationTriggered'
  | 'Processed'
  | 'Rejected'
  | 'Cancelled';

export type CorrectionType =
  | 'SalaryChange'
  | 'AttendanceFix'
  | 'LeaveFix'
  | 'ReimbursementFix'
  | 'TaxFix'
  | 'LoanDeductionFix'
  | 'WrongPayrollRecovery'
  | 'ComplianceFix'
  | 'BonusFix'
  | 'Manual';

export type CorrectionScope = 'DifferentialAdjustment' | 'FullReprocess';

export interface PayrollCorrectionDto {
  id: string;
  employeeId: string;
  employeeName: string;
  correctionType: CorrectionType;
  correctionScope: CorrectionScope;
  status: CorrectionStatus;
  affectedPeriodName: string;
  changeDescription: string;
  differentialAmount: number;
  createdAtUtc: string;
}

export interface SubmitCorrectionRequest {
  employeeId: string;
  employeeName: string;
  correctionType: CorrectionType;
  correctionScope: CorrectionScope;
  affectedPeriodName: string;
  affectedYear: number;
  affectedMonth: number;
  changeDescription: string;
  changeDetails: string;
  afterBankDisbursement: boolean;
  afterTaxFiling: boolean;
  afterEmployeeExit: boolean;
}

// ── Reimbursements ───────────────────────────────────────────────────────────

export type ReimbursementStatus =
  | 'Draft'
  | 'Submitted'
  | 'UnderReview'
  | 'FraudFlagged'
  | 'Approved'
  | 'PartiallyApproved'
  | 'Rejected'
  | 'PaidOut'
  | 'ClawedBack';

export type ReimbursementCategory =
  | 'Travel'
  | 'Meal'
  | 'Internet'
  | 'Fuel'
  | 'Mobile'
  | 'Relocation'
  | 'FlexibleBenefits'
  | 'Medical'
  | 'Books'
  | 'Other';

export interface ReimbursementClaimDto {
  id: string;
  employeeId: string;
  employeeName: string;
  category: ReimbursementCategory;
  status: ReimbursementStatus;
  claimedAmount: number;
  approvedAmount: number;
  paidAmount: number;
  description: string;
  isTaxable: boolean;
  fraudIndicator: string;
  createdAtUtc: string;
}

export interface SubmitReimbursementRequest {
  employeeId: string;
  employeeName: string;
  category: ReimbursementCategory;
  claimedAmount: number;
  description: string;
  attachmentHash?: string;
  claimDate: string;
}

export interface ApproveReimbursementRequest {
  approvedAmount: number;
  remarks?: string;
}

// ── Loans ────────────────────────────────────────────────────────────────────

export type LoanStatus =
  | 'Active'
  | 'PausedInsufficientSalary'
  | 'PreClosed'
  | 'ClosedOnExit'
  | 'FullyRepaid'
  | 'WrittenOff';

export type LoanType =
  | 'SalaryAdvance'
  | 'PersonalLoan'
  | 'VehicleLoan'
  | 'HomeLoan'
  | 'EducationLoan'
  | 'MedicalEmergency'
  | 'Other';

export interface LoanInstallmentDto {
  installmentNumber: number;
  dueDate: string;
  principalAmount: number;
  interestAmount: number;
  totalAmount: number;
  status: string;
  paidDate?: string;
}

export interface EmployeeLoanDto {
  id: string;
  employeeId: string;
  employeeName: string;
  loanType: LoanType;
  status: LoanStatus;
  principalAmount: number;
  interestRate: number;
  tenureMonths: number;
  emiAmount: number;
  disbursedAmount: number;
  outstandingAmount: number;
  paidInstallments: number;
  totalInstallments: number;
  createdAtUtc: string;
}

export interface CreateLoanRequest {
  employeeId: string;
  employeeName: string;
  loanType: LoanType;
  principalAmount: number;
  interestRate: number;
  tenureMonths: number;
  disbursementDate: string;
}

// ── Variable Pay ─────────────────────────────────────────────────────────────

export type VariablePayType =
  | 'PerformanceBonus'
  | 'RetentionBonus'
  | 'SalesIncentive'
  | 'SpotBonus'
  | 'JoiningBonus'
  | 'QuarterlyIncentive'
  | 'AnnualBonus'
  | 'ReferralBonus'
  | 'ProjectBonus';

export type VariablePayStatus =
  | 'Draft'
  | 'PendingApproval'
  | 'Approved'
  | 'Deferred'
  | 'Scheduled'
  | 'PaidOut'
  | 'ExitBeforePayout'
  | 'ClawedBack';

export type TaxTreatment = 'Spread' | 'LumpSum';

export interface VariablePayAllocationDto {
  id: string;
  employeeId: string;
  employeeName: string;
  variablePayType: VariablePayType;
  status: VariablePayStatus;
  allocatedAmount: number;
  proratedAmount: number;
  paidAmount: number;
  payoutPeriodName?: string;
  isClawedBack: boolean;
  createdAtUtc: string;
}

export interface AllocateVariablePayRequest {
  employeeId: string;
  employeeName: string;
  variablePayType: VariablePayType;
  allocatedAmount: number;
  taxTreatment: TaxTreatment;
  performancePeriod: string;
  scheduledPayoutDate?: string;
  clawbackWindowMonths: number;
}

// ── Salary Revisions ─────────────────────────────────────────────────────────

export type RevisionType =
  | 'AnnualIncrement'
  | 'Promotion'
  | 'MarketCorrection'
  | 'RetentionAdjustment'
  | 'OffCycle';

export type RevisionStatus =
  | 'Draft'
  | 'PendingApproval'
  | 'Approved'
  | 'Applied'
  | 'Cancelled';

export interface SalaryRevisionDto {
  id: string;
  employeeId: string;
  employeeName: string;
  revisionType: RevisionType;
  status: RevisionStatus;
  previousCTC: number;
  newCTC: number;
  incrementPercent: number;
  effectiveFrom: string;
  requiresArrears: boolean;
  createdAtUtc: string;
}

export interface CreateRevisionRequest {
  employeeId: string;
  employeeName: string;
  revisionType: RevisionType;
  previousCTC: number;
  newCTC: number;
  effectiveFrom: string;
  requiresArrears: boolean;
  remarks?: string;
}

// ── Simulation ───────────────────────────────────────────────────────────────

export type SimulationType =
  | 'DryRunPayroll'
  | 'RevisionImpact'
  | 'TaxImpact'
  | 'ArrearImpact'
  | 'ComplianceImpact'
  | 'BudgetForecast';

export type SimulationStatus = 'Running' | 'Completed' | 'Failed' | 'Discarded';

export interface SimulationEmployeeResultDto {
  employeeId: string;
  employeeName: string;
  previousGross: number;
  simulatedGross: number;
  previousNet: number;
  simulatedNet: number;
  previousTds: number;
  simulatedTds: number;
  grossDelta: number;
  netDelta: number;
}

export interface PayrollSimulationDto {
  id: string;
  simulationType: SimulationType;
  status: SimulationStatus;
  periodName: string;
  totalEmployees: number;
  totalPreviousGross: number;
  totalSimulatedGross: number;
  employeeResults: SimulationEmployeeResultDto[];
  expiresAtUtc: string;
  createdAtUtc: string;
}

export interface RunSimulationRequest {
  simulationType: SimulationType;
  periodName: string;
  globalCTCAdjustmentPercent?: number;
  employeeOverrides?: Record<string, number>;
}

// ── Disbursement ─────────────────────────────────────────────────────────────

export type DisbursementBatchStatus =
  | 'Pending'
  | 'FileGenerated'
  | 'Submitted'
  | 'PartiallyProcessed'
  | 'Completed'
  | 'Failed'
  | 'Cancelled';

export type BankProvider =
  | 'Hdfc'
  | 'Icici'
  | 'Sbi'
  | 'Axis'
  | 'Kotak'
  | 'Yes'
  | 'Generic';

export interface DisbursementEntryDto {
  employeeId: string;
  employeeName: string;
  bankAccountNumber: string;
  ifscCode: string;
  netAmount: number;
  status: string;
  failureReason?: string;
}

export interface DisbursementBatchDto {
  id: string;
  runId: string;
  periodName: string;
  bankProvider: BankProvider;
  status: DisbursementBatchStatus;
  totalAmount: number;
  entries: DisbursementEntryDto[];
  retryCount: number;
  createdAtUtc: string;
}

export interface InitiateDisbursementRequest {
  runId: string;
  periodName: string;
  bankProvider: BankProvider;
}

// ── Cockpit ──────────────────────────────────────────────────────────────────

export type AnomalyType =
  | 'DuplicatePayout'
  | 'NegativeNetPay'
  | 'MissingDeduction'
  | 'TaxMismatch'
  | 'ArrearMismatch'
  | 'LoanDeductionMismatch'
  | 'FailedDisbursement'
  | 'OrphanedState'
  | 'GrossVarianceHigh';

export type AnomalySeverity = 'Critical' | 'Warning' | 'Info';

export interface PayrollAnomalyDto {
  id: string;
  anomalyType: AnomalyType;
  severity: AnomalySeverity;
  employeeId?: string;
  periodName: string;
  description: string;
  detectedValue?: number;
  expectedValue?: number;
  resolutionStatus: string;
  createdAtUtc: string;
}

export interface ReconciliationJobDto {
  id: string;
  periodName: string;
  status: string;
  employeeCount: number;
  anomalyScore: number;
  criticalCount: number;
  warningCount: number;
  infoCount: number;
  startedAtUtc: string;
  completedAtUtc?: string;
}

export interface PayrollCockpitDto {
  activeRuns: number;
  blockedRuns: number;
  pendingCorrections: number;
  pendingApprovals: number;
  pendingFnF: number;
  failedDisbursements: number;
  openAnomalies: number;
  pendingArrears: number;
}

// ── Paged response ───────────────────────────────────────────────────────────

export interface PagedResult<T> {
  items: T[];
  total: number;
  page: number;
}
