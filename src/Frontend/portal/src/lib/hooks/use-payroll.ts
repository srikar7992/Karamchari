import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { api } from "../api/client";
import type { PayrollRun } from "../types/payroll";

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
