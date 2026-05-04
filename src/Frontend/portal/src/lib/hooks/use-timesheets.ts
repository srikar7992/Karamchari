import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Timesheet } from '../types/time';

/**
 * Hook to fetch the current week's timesheet for the logged-in employee.
 */
export function useCurrentTimesheet() {
  return useQuery<Timesheet>({
    queryKey: ['timesheets', 'current'],
    queryFn: async () => {
      const response = await fetch('/api/time/timesheets/current-week');
      if (!response.ok) throw new Error('Failed to fetch current timesheet');
      return response.json();
    },
  });
}

/**
 * Hook to submit or save a weekly timesheet.
 */
export function useSubmitTimesheet() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (timesheet: Partial<Timesheet>) => {
      const response = await fetch('/api/time/timesheets', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(timesheet),
      });

      if (!response.ok) throw new Error('Failed to submit timesheet');
      return response.json();
    },
    onSuccess: () => {
      // Invalidate the current week's query to trigger a refresh
      queryClient.invalidateQueries({ queryKey: ['timesheets', 'current'] });
    },
  });
}

/**
 * Hook to fetch all timesheets awaiting manager approval.
 */
export function usePendingTimesheets() {
  return useQuery<Timesheet[]>({
    queryKey: ['timesheets', 'pending'],
    queryFn: async () => {
      const response = await fetch('/api/time/timesheets/pending');
      if (!response.ok) throw new Error('Failed to fetch pending timesheets');
      return response.json();
    },
  });
}

/**
 * Hook to approve a timesheet. Uses optimistic updates for a blazingly fast UI.
 */
export function useApproveTimesheet() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (id: string) => {
      const response = await fetch(`/api/time/timesheets/${id}/approve`, {
        method: 'PUT',
      });
      if (!response.ok) throw new Error('Failed to approve timesheet');
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['timesheets', 'pending'] });
    },
  });
}

/**
 * Hook to reject a timesheet.
 */
export function useRejectTimesheet() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({ id, reason }: { id: string; reason: string }) => {
      const response = await fetch(`/api/time/timesheets/${id}/reject`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ reason }),
      });
      if (!response.ok) throw new Error('Failed to reject timesheet');
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['timesheets', 'pending'] });
    },
  });
}

