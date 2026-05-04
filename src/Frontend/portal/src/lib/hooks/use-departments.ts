"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { api } from "@/lib/api/client";
import type { CreateDepartmentCommand, DepartmentListItem } from "@/lib/types/department";

const DEPARTMENTS_PATH = "/api/hr/departments";

/** Stable cache key — append dependent vars (filters, sorts) when they exist. */
export const departmentsKey = ["hr", "departments"] as const;

/**
 * Fetches the active tenant's departments from the BFF.
 * The X-Tenant-Id header is attached by the centralized client.
 */
export function useDepartments() {
  return useQuery<DepartmentListItem[]>({
    queryKey: departmentsKey,
    queryFn: () => api.get<DepartmentListItem[]>(DEPARTMENTS_PATH),
  });
}

/** Optimistic-friendly create — invalidates the list on success. */
export function useCreateDepartment() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (command: CreateDepartmentCommand) =>
      api.post<{ id: string }>(DEPARTMENTS_PATH, { body: command }),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: departmentsKey });
    },
  });
}
