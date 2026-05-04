import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Holiday } from '../types/time';

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
