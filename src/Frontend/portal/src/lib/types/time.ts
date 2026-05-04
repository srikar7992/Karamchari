/**
 * Represents a company holiday.
 */
export interface Holiday {
  /** Unique identifier for the holiday. */
  id: string;
  /** Name of the holiday (e.g., "Christmas"). */
  name: string;
  /** Date of the holiday in ISO format (YYYY-MM-DD). */
  date: string;
}

/**
 * Defines the category of leave.
 */
export enum LeaveCategory {
  /** Employee is paid during leave. */
  Paid = 1,
  /** Employee is not paid during leave. */
  Unpaid = 2,
  /** Legally mandated leave (e.g. Maternity). */
  Statutory = 3
}

/**
 * Defines how leave is accrued.
 */
export enum AccrualFrequency {
  /** Full allowance given at the start of the year. */
  Upfront = 1,
  /** Portion of allowance given each month. */
  Monthly = 2,
  /** Earned based on actual hours worked. */
  HoursWorked = 3
}

/**
 * Represents the detailed rules for a leave policy.
 */
export interface LeavePolicyRules {
  /** The category of leave. */
  category: LeaveCategory;
  /** Annual allowance in days. */
  annualAllowance: number;
  /** Frequency of accrual. */
  accrualFrequency: AccrualFrequency;
  /** Whether manager approval is required. */
  requiresManagerApproval: boolean;
  /** Whether half-days are permitted. */
  allowHalfDays: boolean;
  /** Maximum days that can be carried forward. */
  maximumCarryForward: number;
}

/**
 * Represents an organizational leave policy.
 */
export interface LeavePolicy {
  /** Unique identifier for the policy. */
  id: string;
  /** Name of the leave type. */
  name: string;
  /** Description of the leave type. */
  description: string;
  /** Whether the policy is currently active. */
  isActive: boolean;
  /** Detailed rules for the policy. */
  rules: LeavePolicyRules;
}

/**
 * Tracks the available leave balance for an employee for a specific policy.
 */
export interface LeaveBalance {
  /** Unique identifier for the balance record. */
  id: string;
  /** Employee identifier. */
  employeeId: string;
  /** Policy identifier. */
  policyId: string;
  /** Total entitlement for the period. */
  totalEntitlement: number;
  /** Number of days used. */
  usedBalance: number;
  /** Remaining days available. */
  remainingBalance: number;
}

/**
 * Defines the status of a leave request.
 */
export enum LeaveRequestStatus {
  /** The request is waiting for approval. */
  Pending = 1,
  /** The request has been approved. */
  Approved = 2,
  /** The request has been rejected. */
  Rejected = 3,
  /** The request has been cancelled by the employee. */
  Cancelled = 4
}

/**
 * Represents an employee's request for leave.
 */
export interface LeaveRequest {
  /** Unique identifier for the request. */
  id: string;
  /** Employee identifier. */
  employeeId: string;
  /** Policy identifier. */
  policyId: string;
  /** Start date of the leave. */
  startDate: string;
  /** End date of the leave. */
  endDate: string;
  /** Calculated actual leave days. */
  actualDays: number;
  /** Reason provided by the employee. */
  reason: string | null;
  /** Current status of the request. */
  status: LeaveRequestStatus;
  /** When the request was made. */
  requestedOnUtc: string;
}

/**
 * Defines the status of a weekly timesheet.
 */
export enum TimesheetStatus {
  Draft = 0,
  Submitted = 1,
  Approved = 2,
  Rejected = 3,
}

/**
 * Represents a single day's entry in a timesheet.
 */
export interface TimeEntry {
  date: string;
  hours: number;
  description: string | null;
}

/**
 * Represents a weekly collection of time entries.
 */
export interface Timesheet {
  /** Unique identifier for the timesheet. */
  id: string;
  employeeId: string;
  weekStartDate: string;
  status: TimesheetStatus;
  entries: TimeEntry[];
  totalHours: number;
}

