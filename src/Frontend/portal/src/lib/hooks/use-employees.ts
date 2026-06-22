"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { api } from "@/lib/api/client";
import type { EmployeeListItem, OnboardEmployeeCommand, EmployeeDetail, EmployeeHistoryEvent } from "@/lib/types/employee";

const EMPLOYEES_PATH = "/api/v1/hr/employees";

export const employeesKey = ["hr", "employees"] as const;

export function useEmployees() {
  return useQuery<EmployeeListItem[]>({
    queryKey: employeesKey,
    queryFn: () => api.get<EmployeeListItem[]>(EMPLOYEES_PATH),
  });
}

export function useOnboardEmployee() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (command: OnboardEmployeeCommand) =>
      api.post<{ id: string }>(EMPLOYEES_PATH, { body: command }),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: employeesKey });
    },
  });
}

export function useEmployee(id: string) {
  return useQuery<EmployeeDetail>({
    queryKey: ['hr', 'employees', id],
    queryFn: () => api.get<EmployeeDetail>(`${EMPLOYEES_PATH}/${id}`),
    enabled: !!id,
  });
}

export function useEmployeeHistory(id: string) {
  return useQuery<EmployeeHistoryEvent[]>({
    queryKey: ['hr', 'employees', id, 'history'],
    queryFn: () => api.get<EmployeeHistoryEvent[]>(`${EMPLOYEES_PATH}/${id}/history`),
    enabled: !!id,
  });
}

export function useAnalyzeDocument() {
  return useMutation({
    mutationFn: (file: File) => {
      const formData = new FormData();
      formData.append("file", file);
      
      // We must omit the Content-Type header so fetch can set it to multipart/form-data with the correct boundary
      return api.post<{ legalName: string; dateOfBirth: string | null; idNumber: string }>(
        "/api/hr/documents/analyze-id",
        {
          rawBody: formData,
        }
      );
    },
  });
}
