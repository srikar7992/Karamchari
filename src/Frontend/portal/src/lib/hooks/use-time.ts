import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Holiday, LeavePolicy, LeaveBalance, LeaveRequest } from '../types/time';

/**
 * Hook to fetch all holidays for the current tenant.
 */
export function useHolidays() {
  return useQuery<Holiday[]>({
    queryKey: ['holidays'],
    queryFn: async () => {
      const res = await fetch('/api/time/holidays');
      if (!res.ok) throw new Error('Failed to fetch holidays');
      return res.json();
    },
  });
}

/**
 * Hook to add a new holiday.
 */
export function useAddHoliday() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (holiday: { name: string; date: string }) => {
      const res = await fetch('/api/time/holidays', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(holiday),
      });
      if (!res.ok) throw new Error('Failed to add holiday');
      return res.json();
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['holidays'] });
    },
  });
}

/**
 * Hook to fetch all leave policies for the current tenant.
 */
export function useLeavePolicies() {
  return useQuery<LeavePolicy[]>({
    queryKey: ['leave-policies'],
    queryFn: async () => {
      const res = await fetch('/api/time/leave-policies');
      if (!res.ok) throw new Error('Failed to fetch leave policies');
      return res.json();
    },
  });
}

/**
 * Hook to add a new leave policy.
 */
export function useAddLeavePolicy() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (policy: Partial<LeavePolicy>) => {
      const res = await fetch('/api/time/leave-policies', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(policy),
      });
      if (!res.ok) throw new Error('Failed to add leave policy');
      return res.json();
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['leave-policies'] });
    },
  });
}

/**
 * Hook to fetch leave balances for the current user.
 */
export function useLeaveBalances() {
  return useQuery<LeaveBalance[]>({
    queryKey: ['leave-balances'],
    queryFn: async () => {
      const res = await fetch('/api/time/leave-balances');
      if (!res.ok) throw new Error('Failed to fetch leave balances');
      return res.json();
    },
  });
}

/**
 * Hook to calculate actual leave days for a range (dry-run).
 */
export function useCalculateLeave() {
  return useMutation({
    mutationFn: async (params: { policyId: string; startDate: string; endDate: string }) => {
      const res = await fetch('/api/time/leave-requests/calculate', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(params),
      });
      if (!res.ok) throw new Error('Failed to calculate leave days');
      return res.json() as Promise<{ actualDays: number }>;
    },
  });
}

/**
 * Hook to submit a new leave request.
 */
export function useSubmitLeaveRequest() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (request: { policyId: string; startDate: string; endDate: string; reason: string }) => {
      const res = await fetch('/api/time/leave-requests', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(request),
      });
      if (!res.ok) {
        const errorData = await res.json().catch(() => ({}));
        throw new Error(errorData.message || 'Failed to submit leave request');
      }
      return res.json();
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['leave-balances'] });
      queryClient.invalidateQueries({ queryKey: ['leave-requests'] });
    },
  });
}

/**
 * Hook to fetch all pending leave requests (for managers).
 */
export function usePendingLeaveRequests() {
  return useQuery<LeaveRequest[]>({
    queryKey: ['leave-requests', 'pending'],
    queryFn: async () => {
      const res = await fetch('/api/time/leave-requests/pending');
      if (!res.ok) throw new Error('Failed to fetch pending requests');
      return res.json();
    },
  });
}

/**
 * Hook to approve a leave request.
 */
export function useApproveLeaveRequest() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => {
      const res = await fetch(`/api/time/leave-requests/${id}/approve`, {
        method: 'PUT',
      });
      if (!res.ok) throw new Error('Failed to approve request');
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['leave-requests'] });
    },
  });
}

/**
 * Hook to reject a leave request.
 */
export function useRejectLeaveRequest() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => {
      const res = await fetch(`/api/time/leave-requests/${id}/reject`, {
        method: 'PUT',
      });
      if (!res.ok) throw new Error('Failed to reject request');
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['leave-requests'] });
      queryClient.invalidateQueries({ queryKey: ['leave-balances'] });
    },
  });
}
