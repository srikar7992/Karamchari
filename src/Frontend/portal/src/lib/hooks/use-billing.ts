'use client';

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { api } from '@/lib/api/client';
import type { CollectionCase, ARSummary, ForecastSummary } from '@/lib/types/billing';

const collectionKeys = {
  all: ['collections'] as const,
  filtered: (status?: string) => ['collections', { status }] as const,
};
const arKey       = ['billing', 'ar-summary'] as const;
const forecastKey = ['billing', 'forecast'] as const;

export function useCollectionCases(status?: string) {
  return useQuery({
    queryKey: collectionKeys.filtered(status),
    queryFn: () =>
      api.get<CollectionCase[]>('/api/collections/', {
        params: status ? { status } : {},
      }),
  });
}

export function useARSummary() {
  return useQuery({
    queryKey: arKey,
    queryFn: () => api.get<ARSummary>('/api/billing/ar/summary'),
  });
}

export function useForecastSummary() {
  return useQuery({
    queryKey: forecastKey,
    queryFn: () => api.get<ForecastSummary>('/api/forecast/summary'),
  });
}

export function useSendReminder() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) =>
      api.post<{ lastActionAt: string }>(`/api/collections/${id}/remind`),
    onSuccess: () => qc.invalidateQueries({ queryKey: collectionKeys.all }),
  });
}

export function useMarkDisputed() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => api.put<void>(`/api/collections/${id}/dispute`),
    onSuccess: () => qc.invalidateQueries({ queryKey: collectionKeys.all }),
  });
}
