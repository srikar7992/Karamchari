import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { api } from "../api/client";
import type {
  FnFSettlementDto,
  InitiateFnFRequest,
  ArrearCalculationDto,
  TriggerArrearRequest,
  PayrollCorrectionDto,
  SubmitCorrectionRequest,
  ReimbursementClaimDto,
  SubmitReimbursementRequest,
  ApproveReimbursementRequest,
  EmployeeLoanDto,
  CreateLoanRequest,
  VariablePayAllocationDto,
  AllocateVariablePayRequest,
  SalaryRevisionDto,
  CreateRevisionRequest,
  PayrollSimulationDto,
  RunSimulationRequest,
  DisbursementBatchDto,
  InitiateDisbursementRequest,
  PayrollCockpitDto,
  ReconciliationJobDto,
  PayrollAnomalyDto,
  PagedResult,
} from "../types/payroll-phase1a";

// ── FnF ─────────────────────────────────────────────────────────────────────

const FNF = "/api/v1/payroll/fnf";

export const fnfKeys = {
  all: ["fnf"] as const,
  detail: (id: string) => ["fnf", id] as const,
};

export function useFnFSettlements(params?: { status?: string; employeeId?: string }) {
  return useQuery({
    queryKey: [...fnfKeys.all, params],
    queryFn: () => api.get<PagedResult<FnFSettlementDto>>(FNF, { params }),
  });
}

export function useFnFSettlement(id: string) {
  return useQuery({
    queryKey: fnfKeys.detail(id),
    queryFn: () => api.get<FnFSettlementDto>(`${FNF}/${id}`),
    enabled: !!id,
  });
}

export function useInitiateFnF() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (req: InitiateFnFRequest) =>
      api.post<FnFSettlementDto>(FNF, { body: req }),
    onSuccess: () => void qc.invalidateQueries({ queryKey: fnfKeys.all }),
  });
}

export function useFnFAction(id: string, action: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body?: unknown) =>
      api.post<FnFSettlementDto>(`${FNF}/${id}/${action}`, body ? { body } : undefined),
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: fnfKeys.detail(id) });
      void qc.invalidateQueries({ queryKey: fnfKeys.all });
    },
  });
}

// ── Arrears ─────────────────────────────────────────────────────────────────

const ARREARS = "/api/v1/payroll/arrears";

export const arrearKeys = {
  all: ["arrears"] as const,
  detail: (id: string) => ["arrears", id] as const,
};

export function useArrears(params?: { employeeId?: string; status?: string }) {
  return useQuery({
    queryKey: [...arrearKeys.all, params],
    queryFn: () => api.get<PagedResult<ArrearCalculationDto>>(ARREARS, { params }),
  });
}

export function useArrear(id: string) {
  return useQuery({
    queryKey: arrearKeys.detail(id),
    queryFn: () => api.get<ArrearCalculationDto>(`${ARREARS}/${id}`),
    enabled: !!id,
  });
}

export function useTriggerArrear() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (req: TriggerArrearRequest) =>
      api.post<ArrearCalculationDto>(`${ARREARS}/trigger`, { body: req }),
    onSuccess: () => void qc.invalidateQueries({ queryKey: arrearKeys.all }),
  });
}

export function useApproveArrear(id: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: () => api.post<ArrearCalculationDto>(`${ARREARS}/${id}/approve`),
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: arrearKeys.detail(id) });
      void qc.invalidateQueries({ queryKey: arrearKeys.all });
    },
  });
}

// ── Corrections ─────────────────────────────────────────────────────────────

const CORRECTIONS = "/api/v1/payroll/corrections";

export const correctionKeys = {
  all: ["corrections"] as const,
  detail: (id: string) => ["corrections", id] as const,
};

export function useCorrections(params?: { status?: string; employeeId?: string }) {
  return useQuery({
    queryKey: [...correctionKeys.all, params],
    queryFn: () => api.get<PagedResult<PayrollCorrectionDto>>(CORRECTIONS, { params }),
  });
}

export function useSubmitCorrection() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (req: SubmitCorrectionRequest) =>
      api.post<PayrollCorrectionDto>(CORRECTIONS, { body: req }),
    onSuccess: () => void qc.invalidateQueries({ queryKey: correctionKeys.all }),
  });
}

export function useCorrectionAction(id: string, action: "submit" | "approve" | "reject") {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body?: string) =>
      api.post<PayrollCorrectionDto>(`${CORRECTIONS}/${id}/${action}`, body ? { body } : undefined),
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: correctionKeys.detail(id) });
      void qc.invalidateQueries({ queryKey: correctionKeys.all });
    },
  });
}

// ── Reimbursements ───────────────────────────────────────────────────────────

const REIMB = "/api/v1/payroll/reimbursements";

export const reimbursementKeys = {
  all: ["reimbursements"] as const,
  mine: ["reimbursements", "mine"] as const,
  detail: (id: string) => ["reimbursements", id] as const,
};

export function useReimbursements(params?: { status?: string; category?: string }) {
  return useQuery({
    queryKey: [...reimbursementKeys.all, params],
    queryFn: () => api.get<PagedResult<ReimbursementClaimDto>>(REIMB, { params }),
  });
}

export function useMyReimbursements() {
  return useQuery({
    queryKey: reimbursementKeys.mine,
    queryFn: () => api.get<PagedResult<ReimbursementClaimDto>>(`${REIMB}/my`),
  });
}

export function useSubmitReimbursement() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (req: SubmitReimbursementRequest) =>
      api.post<ReimbursementClaimDto>(REIMB, { body: req }),
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: reimbursementKeys.all });
      void qc.invalidateQueries({ queryKey: reimbursementKeys.mine });
    },
  });
}

export function useApproveReimbursement(id: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (req: ApproveReimbursementRequest) =>
      api.post<ReimbursementClaimDto>(`${REIMB}/${id}/approve`, { body: req }),
    onSuccess: () => void qc.invalidateQueries({ queryKey: reimbursementKeys.all }),
  });
}

export function useRejectReimbursement(id: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (reason: string) =>
      api.post<ReimbursementClaimDto>(`${REIMB}/${id}/reject`, { body: reason }),
    onSuccess: () => void qc.invalidateQueries({ queryKey: reimbursementKeys.all }),
  });
}

// ── Loans ────────────────────────────────────────────────────────────────────

const LOANS = "/api/v1/payroll/loans";

export const loanKeys = {
  all: ["loans"] as const,
  detail: (id: string) => ["loans", id] as const,
};

export function useLoans(params?: { employeeId?: string; status?: string }) {
  return useQuery({
    queryKey: [...loanKeys.all, params],
    queryFn: () => api.get<PagedResult<EmployeeLoanDto>>(LOANS, { params }),
  });
}

export function useLoan(id: string) {
  return useQuery({
    queryKey: loanKeys.detail(id),
    queryFn: () => api.get<EmployeeLoanDto>(`${LOANS}/${id}`),
    enabled: !!id,
  });
}

export function useCreateLoan() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (req: CreateLoanRequest) =>
      api.post<EmployeeLoanDto>(LOANS, { body: req }),
    onSuccess: () => void qc.invalidateQueries({ queryKey: loanKeys.all }),
  });
}

export function usePreCloseLoan(id: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: () => api.post<EmployeeLoanDto>(`${LOANS}/${id}/pre-close`),
    onSuccess: () => void qc.invalidateQueries({ queryKey: loanKeys.detail(id) }),
  });
}

// ── Variable Pay ─────────────────────────────────────────────────────────────

const VARPAY = "/api/v1/payroll/variable-pay";

export const variablePayKeys = {
  all: ["variablePay"] as const,
  detail: (id: string) => ["variablePay", id] as const,
};

export function useVariablePayAllocations(params?: { employeeId?: string; status?: string }) {
  return useQuery({
    queryKey: [...variablePayKeys.all, params],
    queryFn: () => api.get<PagedResult<VariablePayAllocationDto>>(VARPAY, { params }),
  });
}

export function useAllocateVariablePay() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (req: AllocateVariablePayRequest) =>
      api.post<VariablePayAllocationDto>(VARPAY, { body: req }),
    onSuccess: () => void qc.invalidateQueries({ queryKey: variablePayKeys.all }),
  });
}

export function useApproveVariablePay(id: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: () => api.post<VariablePayAllocationDto>(`${VARPAY}/${id}/approve`),
    onSuccess: () => void qc.invalidateQueries({ queryKey: variablePayKeys.all }),
  });
}

// ── Salary Revisions ─────────────────────────────────────────────────────────

const REVISIONS = "/api/v1/payroll/revisions";

export const revisionKeys = {
  all: ["revisions"] as const,
  detail: (id: string) => ["revisions", id] as const,
};

export function useSalaryRevisions(params?: { employeeId?: string; status?: string }) {
  return useQuery({
    queryKey: [...revisionKeys.all, params],
    queryFn: () => api.get<PagedResult<SalaryRevisionDto>>(REVISIONS, { params }),
  });
}

export function useCreateRevision() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (req: CreateRevisionRequest) =>
      api.post<SalaryRevisionDto>(REVISIONS, { body: req }),
    onSuccess: () => void qc.invalidateQueries({ queryKey: revisionKeys.all }),
  });
}

export function useApproveRevision(id: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: () => api.post<SalaryRevisionDto>(`${REVISIONS}/${id}/approve`),
    onSuccess: () => void qc.invalidateQueries({ queryKey: revisionKeys.all }),
  });
}

// ── Simulation ───────────────────────────────────────────────────────────────

const SIM = "/api/v1/payroll/simulations";

export const simulationKeys = {
  all: ["simulations"] as const,
  detail: (id: string) => ["simulations", id] as const,
};

export function useRunSimulation() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (req: RunSimulationRequest) =>
      api.post<PayrollSimulationDto>(SIM, { body: req }),
    onSuccess: () => void qc.invalidateQueries({ queryKey: simulationKeys.all }),
  });
}

export function useSimulation(id: string) {
  return useQuery({
    queryKey: simulationKeys.detail(id),
    queryFn: () => api.get<PayrollSimulationDto>(`${SIM}/${id}`),
    enabled: !!id,
    refetchInterval: (query) => {
      const sim = query.state.data;
      return sim?.status === "Running" ? 3000 : false;
    },
  });
}

export function useDiscardSimulation(id: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: () => api.post<void>(`${SIM}/${id}/discard`),
    onSuccess: () => void qc.invalidateQueries({ queryKey: simulationKeys.all }),
  });
}

// ── Disbursement ─────────────────────────────────────────────────────────────

const DISB = "/api/v1/payroll/disbursements";

export const disbursementKeys = {
  all: ["disbursements"] as const,
  detail: (id: string) => ["disbursements", id] as const,
};

export function useDisbursements(params?: { status?: string }) {
  return useQuery({
    queryKey: [...disbursementKeys.all, params],
    queryFn: () => api.get<PagedResult<DisbursementBatchDto>>(DISB, { params }),
  });
}

export function useDisbursement(id: string) {
  return useQuery({
    queryKey: disbursementKeys.detail(id),
    queryFn: () => api.get<DisbursementBatchDto>(`${DISB}/${id}`),
    enabled: !!id,
    refetchInterval: (query) => {
      const batch = query.state.data;
      return batch?.status === "Submitted" ? 5000 : false;
    },
  });
}

export function useInitiateDisbursement() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (req: InitiateDisbursementRequest) =>
      api.post<DisbursementBatchDto>(DISB, { body: req }),
    onSuccess: () => void qc.invalidateQueries({ queryKey: disbursementKeys.all }),
  });
}

export function useRetryDisbursement(id: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: () => api.post<DisbursementBatchDto>(`${DISB}/${id}/retry`),
    onSuccess: () => void qc.invalidateQueries({ queryKey: disbursementKeys.detail(id) }),
  });
}

// ── Cockpit ──────────────────────────────────────────────────────────────────

const COCKPIT = "/api/v1/payroll/cockpit";

export function usePayrollCockpit() {
  return useQuery({
    queryKey: ["payrollCockpit"],
    queryFn: () => api.get<PayrollCockpitDto>(COCKPIT),
    refetchInterval: 30_000,
  });
}

export function useReconciliationJobs(params?: { status?: string }) {
  return useQuery({
    queryKey: ["reconciliationJobs", params],
    queryFn: () => api.get<PagedResult<ReconciliationJobDto>>(`${COCKPIT}/reconciliation`, { params }),
  });
}

export function useRunReconciliation() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (periodName: string) =>
      api.post<ReconciliationJobDto>(`${COCKPIT}/reconciliation/run`, { body: { periodName } }),
    onSuccess: () => void qc.invalidateQueries({ queryKey: ["reconciliationJobs"] }),
  });
}

export function useAnomalies(params?: { severity?: string; resolved?: boolean }) {
  return useQuery({
    queryKey: ["anomalies", params],
    queryFn: () => api.get<PagedResult<PayrollAnomalyDto>>(`${COCKPIT}/anomalies`, { params }),
  });
}

export function useResolveAnomaly(id: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (resolution: string) =>
      api.post<PayrollAnomalyDto>(`${COCKPIT}/anomalies/${id}/resolve`, { body: { resolution } }),
    onSuccess: () => void qc.invalidateQueries({ queryKey: ["anomalies"] }),
  });
}
