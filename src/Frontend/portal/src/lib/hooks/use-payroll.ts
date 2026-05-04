import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { api } from "../api/client";
import type { PayrollRun, PayrollRunDetail } from "../types/payroll";

const PAYROLL_RUNS_PATH = "/api/payroll/runs";
export const payrollRunsKey = ["payrollRuns"];

export function usePayrollRuns() {
  return useQuery({
    queryKey: payrollRunsKey,
    queryFn: () => api.get<PayrollRun[]>(PAYROLL_RUNS_PATH),
    refetchInterval: (query) => {
      // Poll every 5 seconds if any run is in the "Calculating" state
      const runs = query.state.data;
      if (runs && runs.some((run) => run.currentState === "Calculating")) {
        return 5000;
      }
      return false; // Disable polling otherwise
    },
  });
}

export function useStartPayrollRun() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (periodName: string) =>
      api.post<{ runId: string }>(PAYROLL_RUNS_PATH, {
        body: { periodName },
      }),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: payrollRunsKey });
    },
  });
}

export function usePayrollRunSummary(runId: string) {
  return useQuery({
    queryKey: [...payrollRunsKey, runId, "summary"],
    queryFn: () => api.get<PayrollRun & { totalTax: number }>(`${PAYROLL_RUNS_PATH}/${runId}/summary`),
    enabled: !!runId,
    refetchInterval: (query) => {
      const summary = query.state.data;
      if (summary && summary.currentState === "Calculating") return 5000;
      return false;
    }
  });
}

export function usePayrollRunDetails(runId: string) {
  return useQuery({
    queryKey: [...payrollRunsKey, runId, "details"],
    queryFn: () => api.get<PayrollRunDetail[]>(`${PAYROLL_RUNS_PATH}/${runId}/details`),
    enabled: !!runId,
  });
}

export function useLockPayrollRun() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (runId: string) =>
      api.put(`${PAYROLL_RUNS_PATH}/${runId}/lock`, {
        body: { approvedBy: "HR Admin" }, // Placeholder for actual user session
      }),
    onSuccess: (_, runId) => {
      void queryClient.invalidateQueries({ queryKey: payrollRunsKey });
      void queryClient.invalidateQueries({ queryKey: [...payrollRunsKey, runId, "summary"] });
    },
  });
}
