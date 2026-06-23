"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { api } from "@/lib/api/client";
import type {
  ApplyCandidateRequest,
  CandidateDetailDto,
  CandidateSummaryDto,
  ApplicationDetailDto,
  ApplicationSummaryDto,
  CreateCandidateRequest,
  CreateOfferRequest,
  IssueOfferRequest,
  OfferDto,
  PipelineDto,
  ScheduleInterviewRequest,
  SubmitFeedbackRequest,
  TimelineEntryDto,
} from "@/lib/types/recruitment";

const RECRUITMENT_PATH = "/api/v1/recruitment";

export const recruitmentKeys = {
  all: ["recruitment"] as const,
  pipeline: ["recruitment", "pipeline"] as const,
  candidates: ["recruitment", "candidates"] as const,
  candidate: (id: string) => ["recruitment", "candidates", id] as const,
  timeline: (id: string) => ["recruitment", "candidates", id, "timeline"] as const,
  applications: ["recruitment", "applications"] as const,
  application: (id: string) => ["recruitment", "applications", id] as const,
  offers: ["recruitment", "offers"] as const,
  offer: (id: string) => ["recruitment", "offers", id] as const,
} as const;

// ── Reads ──────────────────────────────────────────────────────────────
export function useCandidates() {
  return useQuery<CandidateSummaryDto[]>({
    queryKey: recruitmentKeys.candidates,
    queryFn: () => api.get<CandidateSummaryDto[]>(`${RECRUITMENT_PATH}/candidates`),
  });
}

export function useCandidate(id: string) {
  return useQuery<CandidateDetailDto>({
    queryKey: recruitmentKeys.candidate(id),
    queryFn: () => api.get<CandidateDetailDto>(`${RECRUITMENT_PATH}/candidates/${id}`),
    enabled: !!id,
  });
}

export function useCandidateTimeline(id: string) {
  return useQuery<TimelineEntryDto[]>({
    queryKey: recruitmentKeys.timeline(id),
    queryFn: () => api.get<TimelineEntryDto[]>(`${RECRUITMENT_PATH}/candidates/${id}/timeline`),
    enabled: !!id,
  });
}

export interface ApplicationsFilter {
  candidateId?: string;
  requisitionId?: string;
  status?: string;
}

export function useApplications(filter: ApplicationsFilter = {}) {
  return useQuery<ApplicationSummaryDto[]>({
    queryKey: [...recruitmentKeys.applications, filter],
    queryFn: () =>
      api.get<ApplicationSummaryDto[]>(`${RECRUITMENT_PATH}/applications`, {
        params: {
          candidateId: filter.candidateId,
          requisitionId: filter.requisitionId,
          status: filter.status,
        } as Record<string, string | undefined>,
      }),
  });
}

export function useApplication(id: string) {
  return useQuery<ApplicationDetailDto>({
    queryKey: recruitmentKeys.application(id),
    queryFn: () => api.get<ApplicationDetailDto>(`${RECRUITMENT_PATH}/applications/${id}`),
    enabled: !!id,
  });
}

export interface OffersFilter {
  applicationId?: string;
  status?: string;
}

export function useOffers(filter: OffersFilter = {}) {
  return useQuery<OfferDto[]>({
    queryKey: [...recruitmentKeys.offers, filter],
    queryFn: () =>
      api.get<OfferDto[]>(`${RECRUITMENT_PATH}/offers`, {
        params: {
          applicationId: filter.applicationId,
          status: filter.status,
        } as Record<string, string | undefined>,
      }),
  });
}

export function useOffer(id: string) {
  return useQuery<OfferDto>({
    queryKey: recruitmentKeys.offer(id),
    queryFn: () => api.get<OfferDto>(`${RECRUITMENT_PATH}/offers/${id}`),
    enabled: !!id,
  });
}

export function usePipeline() {
  return useQuery<PipelineDto>({
    queryKey: recruitmentKeys.pipeline,
    queryFn: () => api.get<PipelineDto>(`${RECRUITMENT_PATH}/pipeline`),
  });
}

// ── Writes ─────────────────────────────────────────────────────────────
function useInvalidateAll() {
  const queryClient = useQueryClient();
  return () => void queryClient.invalidateQueries({ queryKey: recruitmentKeys.all });
}

export function useCreateCandidate() {
  const invalidateAll = useInvalidateAll();
  return useMutation({
    mutationFn: (command: CreateCandidateRequest) =>
      api.post<{ id: string }>(`${RECRUITMENT_PATH}/candidates`, { body: command }),
    onSuccess: invalidateAll,
  });
}

export function useApplyCandidate() {
  const invalidateAll = useInvalidateAll();
  return useMutation({
    mutationFn: (command: ApplyCandidateRequest) =>
      api.post<{ id: string }>(`${RECRUITMENT_PATH}/applications`, { body: command }),
    onSuccess: invalidateAll,
  });
}

export function useAdvanceApplication(id: string) {
  const invalidateAll = useInvalidateAll();
  return useMutation({
    mutationFn: () => api.post<void>(`${RECRUITMENT_PATH}/applications/${id}/advance`),
    onSuccess: invalidateAll,
  });
}

export function useScheduleInterview() {
  const invalidateAll = useInvalidateAll();
  return useMutation({
    mutationFn: (command: ScheduleInterviewRequest) =>
      api.post<{ id: string }>(`${RECRUITMENT_PATH}/interviews`, { body: command }),
    onSuccess: invalidateAll,
  });
}

export function useSubmitFeedback(interviewId: string) {
  const invalidateAll = useInvalidateAll();
  return useMutation({
    mutationFn: (command: SubmitFeedbackRequest) =>
      api.post<{ id: string }>(`${RECRUITMENT_PATH}/interviews/${interviewId}/feedback`, {
        body: command,
      }),
    onSuccess: invalidateAll,
  });
}

export function useCreateOffer() {
  const invalidateAll = useInvalidateAll();
  return useMutation({
    mutationFn: (command: CreateOfferRequest) =>
      api.post<{ id: string }>(`${RECRUITMENT_PATH}/offers`, { body: command }),
    onSuccess: invalidateAll,
  });
}

export function useApproveOffer(id: string) {
  const invalidateAll = useInvalidateAll();
  return useMutation({
    mutationFn: () => api.post<void>(`${RECRUITMENT_PATH}/offers/${id}/approve`),
    onSuccess: invalidateAll,
  });
}

export function useIssueOffer(id: string) {
  const invalidateAll = useInvalidateAll();
  return useMutation({
    mutationFn: (command: IssueOfferRequest) =>
      api.post<void>(`${RECRUITMENT_PATH}/offers/${id}/issue`, { body: command }),
    onSuccess: invalidateAll,
  });
}

export function useAcceptOffer(id: string) {
  const invalidateAll = useInvalidateAll();
  return useMutation({
    mutationFn: () => api.post<void>(`${RECRUITMENT_PATH}/offers/${id}/accept`),
    onSuccess: invalidateAll,
  });
}

export function useHireCandidate(applicationId: string) {
  const invalidateAll = useInvalidateAll();
  return useMutation({
    mutationFn: () => api.post<void>(`${RECRUITMENT_PATH}/applications/${applicationId}/hire`),
    onSuccess: invalidateAll,
  });
}