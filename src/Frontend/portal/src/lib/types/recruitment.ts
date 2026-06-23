// ── Recruitment domain types ────────────────────────────────────────────
// Mirror of Karamchari.Recruitment.Contracts DTOs (Phase 1A read model).
// Case-mapping follows the ASP.NET Core HTTP default (camelCase).

// ── Enum unions (mirror Karamchari.Recruitment.Domain enums) ────────────
export type ApplicationStatus =
  | "New"
  | "Screening"
  | "Interviewing"
  | "Offered"
  | "Hired"
  | "Rejected"
  | "Withdrawn";

export type InterviewStatus = "Scheduled" | "Completed" | "Cancelled" | "NoShow";

export type OfferStatus =
  | "Draft"
  | "PendingApproval"
  | "Approved"
  | "Issued"
  | "Accepted"
  | "Declined"
  | "Expired"
  | "Rescinded";

// ── Requisition / candidate / application / interview / offer ──────────
export interface RequisitionDto {
  id: string;
  title: string;
  departmentId: string;
  hiringManagerId: string;
  status: string;
  targetHireDate: string | null;
}

export interface CandidateSummaryDto {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber: string | null;
  profileVersion: number;
  createdOnUtc: string;
}

export interface CandidateSnapshotDto {
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber: string | null;
  version: number;
}

export interface ApplicationSummaryDto {
  id: string;
  candidateId: string;
  requisitionId: string;
  status: string;
  appliedAt: string;
  hiredAt: string | null;
  hiredBy: string | null;
}

export interface InterviewFeedbackDto {
  id: string;
  interviewerId: string;
  rating: number;
  comments: string;
  submittedAt: string;
}

export interface InterviewDto {
  id: string;
  applicationId: string;
  scheduledAt: string;
  durationMinutes: number;
  status: string;
  interviewerIds: string[];
  feedback: InterviewFeedbackDto[];
}

export interface OfferDto {
  id: string;
  applicationId: string;
  baseSalary: number;
  currency: string;
  status: OfferStatus;
  issuedAt: string | null;
  expiresAt: string | null;
}

export interface TimelineEntryDto {
  id: string;
  entityType: string;
  entityId: string;
  action: string;
  oldState: string | null;
  newState: string;
  timestamp: string;
  userId: string;
}

export interface CandidateDetailDto {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber: string | null;
  profileVersion: number;
  createdOnUtc: string;
  updatedOnUtc: string | null;
  applications: ApplicationSummaryDto[];
  interviews: InterviewDto[];
  offers: OfferDto[];
  timeline: TimelineEntryDto[];
}

export interface ApplicationDetailDto {
  id: string;
  candidateId: string;
  requisitionId: string;
  status: string;
  appliedAt: string;
  hiredAt: string | null;
  hiredBy: string | null;
  candidate: CandidateSnapshotDto | null;
  interviews: InterviewDto[];
  offers: OfferDto[];
  timeline: TimelineEntryDto[];
}

// ── Pipeline projection ────────────────────────────────────────────────
export interface PipelineCandidateDto {
  applicationId: string;
  candidateId: string;
  candidateName: string;
  email: string;
  phoneNumber: string | null;
  requisitionId: string;
  appliedAt: string;
  stage: string;
}

export interface PipelineStageDto {
  stage: string;
  cards: PipelineCandidateDto[];
}

export interface PipelineDto {
  stages: PipelineStageDto[];
  stageCounts: Record<string, number>;
}

// ── Commands (writes) ─────────────────────────────────────────────────
export interface CreateCandidateRequest {
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber?: string | null;
}

export interface ApplyCandidateRequest {
  candidateId: string;
  requisitionId: string;
}

export interface ScheduleInterviewRequest {
  applicationId: string;
  scheduledAt: string;
  durationMinutes: number;
  interviewerIds: string[];
}

export interface SubmitFeedbackRequest {
  interviewerId: string;
  rating: number;
  comments: string;
}

export interface CreateOfferRequest {
  applicationId: string;
  baseSalary: number;
  currency: string;
}

export interface IssueOfferRequest {
  expiresAt: string;
}