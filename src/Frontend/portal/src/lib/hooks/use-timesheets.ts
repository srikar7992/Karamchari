import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { api } from '@/lib/api/client';
import { Timesheet } from '../types/time';

const timesheetKeys = {
  current: ['timesheets', 'current'] as const,
};

export function useCurrentTimesheet() {
  return useQuery<Timesheet>({
    queryKey: timesheetKeys.current,
    queryFn: () => api.get<Timesheet>('/api/v1/time/timesheets/current'),
  });
}

export function useSubmitTimesheet() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (timesheet: Partial<Timesheet>) =>
      api.post<Timesheet>('/api/v1/time/timesheets', { body: timesheet }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: timesheetKeys.current });
    },
  });
}

export function usePendingTimesheets() {
  return useQuery<Timesheet[]>({
    queryKey: ['timesheets', 'pending'],
    queryFn: () => api.get<Timesheet[]>('/api/v1/time/timesheets/pending'),
  });
}

export function useApproveTimesheet() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => api.post<void>(`/api/v1/time/timesheets/${id}/approve`),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['timesheets', 'pending'] });
    },
  });
}

export function useRejectTimesheet() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, reason }: { id: string; reason: string }) =>
      api.post<void>(`/api/v1/time/timesheets/${id}/reject`, { body: { reason } }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['timesheets', 'pending'] });
    },
  });
}
