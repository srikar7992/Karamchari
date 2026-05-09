export interface StartPayrollRunRequest {
  periodName: string;
}

export interface PayrollRunSummary {
  correlationId: string;
  periodName: string;
  currentState: string;
  totalEmployeesToProcess: number;
  processedEmployees: number;
  totalGross: number;
  totalNet: number;
  totalTax: number;
  startedAt: string;
  lockedAt?: string;
  lockedBy?: string;
}

export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  extensions?: Record<string, any>;
}

export interface ApiErrorPayload {
  message?: string;
  reason?: string;
  errors?: Record<string, string[]>;
}
