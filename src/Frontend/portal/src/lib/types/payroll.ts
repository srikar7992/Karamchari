export interface PayrollRun {
  id: string;
  periodName: string;
  currentState: string;
  startedAt: string;
  completedAt: string | null;
  totalEmployeesToProcess: number;
  processedEmployees: number;
}
