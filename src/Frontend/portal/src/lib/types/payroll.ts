export interface PayrollRun {
  id: string; // Internal EF ID (if any)
  correlationId: string; // Saga ID
  periodName: string;
  currentState: string;
  startedAt: string;
  completedAt: string | null;
  totalEmployeesToProcess: number;
  processedEmployees: number;
  totalGross: number;
  totalNet: number;
  lockedBy: string | null;
  lockedAt: string | null;
}

export interface PayrollRunDetail {
  employeeId: string;
  monthlyGross: number;
  netPay: number;
  tds: number;
  variancePercentage: number;
  isAnomaly: boolean;
}
