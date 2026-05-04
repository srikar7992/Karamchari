import { useQuery, useMutation } from "@tanstack/react-query";
import { api } from "../api/client";

export function useTaxSimulation() {
  return useMutation({
    mutationFn: (data: { 
      section80C: number; 
      section80D: number; 
      monthlyRent: number; 
      isMetro: boolean; 
      financialYear: number 
    }) => api.post<any>("/api/ess/tax-simulator/dry-run", { body: data }),
  });
}

export function useAnalyzeProof() {
  return useMutation<any, Error, File>({
    mutationFn: (file: File) => {
      const formData = new FormData();
      formData.append("file", file);
      // We use rawBody for FormData to bypass JSON serialization in client.ts
      return api.post<any>("/api/ess/declarations/analyze", { rawBody: formData });
    },
  });
}




export interface EssPayslip {
  id: string;
  year: number;
  month: number;
  periodName: string;
  monthlyGross: number;
  netPay: number;
  tdsDeducted: number;
}

export interface YtdSummary {
  grossYtd: number;
  netYtd: number;
  taxYtd: number;
  count: number;
}

const ESS_PAYSLIPS_PATH = "/api/ess/payslips";

export function useEssPayslips() {
  return useQuery({
    queryKey: ["ess", "payslips"],
    queryFn: () => api.get<EssPayslip[]>(ESS_PAYSLIPS_PATH),
  });
}

export function useYtdSummary() {
  return useQuery({
    queryKey: ["ess", "ytd"],
    queryFn: () => api.get<YtdSummary>(`${ESS_PAYSLIPS_PATH}/ytd`),
  });
}

export function getPayslipDownloadUrl(year: number, month: number) {
  // In a real app, we'd use the base URL from config
  const baseUrl = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5000";
  return `${baseUrl}${ESS_PAYSLIPS_PATH}/${year}/${month}/download`;
}
