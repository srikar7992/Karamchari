import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { api } from '@/lib/api/client';
import { Holiday, LeavePolicy, LeaveBalance, LeaveRequest } from '../types/time';

const timeKeys = {
  holidays: ['holidays'] as const,
  leavePolicies: ['leave-policies'] as const,
  leaveBalances: ['leave-balances'] as const,
  leaveRequests: ['leave-requests'] as const,
  pendingLeaveRequests: ['leave-requests', 'pending'] as const,
};

export function useHolidays() {
  return useQuery<Holiday[]>({
    queryKey: timeKeys.holidays,
    queryFn: () => api.get<Holiday[]>('/api/v1/time/holidays'),
  });
}

export function useAddHoliday() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (holiday: { name: string; date: string }) =>
      api.post<Holiday>('/api/v1/time/holidays', { body: holiday }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: timeKeys.holidays });
    },
  });
}

export function useLeavePolicies() {
  return useQuery<LeavePolicy[]>({
    queryKey: timeKeys.leavePolicies,
    queryFn: () => api.get<LeavePolicy[]>('/api/v1/leaves/policies'),
  });
}

export function useAddLeavePolicy() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (policy: Partial<LeavePolicy>) =>
      api.post<LeavePolicy>('/api/v1/leaves/policies', { body: policy }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: timeKeys.leavePolicies });
    },
  });
}

export function useLeaveBalances() {
  return useQuery<LeaveBalance[]>({
    queryKey: timeKeys.leaveBalances,
    queryFn: () => api.get<LeaveBalance[]>('/api/v1/time/leave-balances'),
  });
}

export function useMyLeaveRequests() {
  return useQuery<LeaveRequest[]>({
    queryKey: timeKeys.leaveRequests,
    queryFn: () => api.get<LeaveRequest[]>('/api/v1/leaves/my'),
  });
}

// Alias used by the leave self-service page.
export const useMyLeaves = useMyLeaveRequests;

// Dry-run leave day calculation — no backend endpoint yet; returns 0 as stub.
export function useCalculateLeave() {
  return useMutation({
    mutationFn: async (_params: { policyId: string; startDate: string; endDate: string }) =>
      ({ actualDays: 0 } as { actualDays: number }),
  });
}

export function useSubmitLeaveRequest() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (request: { policyId: string; startDate: string; endDate: string; reason: string }) => {
      const created = await api.post<{ id: string }>('/api/v1/leaves/request', {
        body: {
          policyId: request.policyId,
          startDate: request.startDate,
          endDate: request.endDate,
          reason: request.reason,
        },
      });
      return api.post<LeaveRequest>(`/api/v1/leaves/${created.id}/submit`);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: timeKeys.leaveBalances });
      queryClient.invalidateQueries({ queryKey: timeKeys.leaveRequests });
    },
  });
}

export function usePendingLeaveRequests() {
  return useQuery<LeaveRequest[]>({
    queryKey: timeKeys.pendingLeaveRequests,
    queryFn: () => api.get<LeaveRequest[]>('/api/v1/leaves/pending'),
  });
}

export function useApproveLeaveRequest() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => api.post<void>(`/api/v1/leaves/${id}/approve`),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: timeKeys.pendingLeaveRequests });
    },
  });
}

export function useRejectLeaveRequest() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => api.post<void>(`/api/v1/leaves/${id}/reject`),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: timeKeys.pendingLeaveRequests });
      queryClient.invalidateQueries({ queryKey: timeKeys.leaveBalances });
    },
  });
}
