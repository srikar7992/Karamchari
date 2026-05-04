import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { api } from "../api/client";

export interface PendingDeclaration {
  id: string;
  employeeId: string;
  category: string;
  claimedAmount: number;
  submittedAt: string;
  proofUri: string;
}

export function usePendingDeclarations() {
  return useQuery({
    queryKey: ["admin", "declarations", "pending"],
    queryFn: () => api.get<PendingDeclaration[]>("/api/admin/declarations/pending"),
  });
}

export function useApproveDeclaration() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, amount }: { id: string; amount: number }) => 
      api.put(`/api/admin/declarations/${id}/approve`, { body: amount }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["admin", "declarations", "pending"] });
    }
  });
}

export function useRejectDeclaration() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, reason }: { id: string; reason: string }) => 
      api.put(`/api/admin/declarations/${id}/reject`, { body: reason }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["admin", "declarations", "pending"] });
    }
  });
}

export function getDeclarationDocumentUrl(id: string) {
  const baseUrl = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5000";
  return `${baseUrl}/api/admin/declarations/${id}/document`;
}
