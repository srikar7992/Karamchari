/**
 * Attendance Feature Type Definitions.
 */

export type AttendanceStatus = "Present" | "Late" | "Absent" | "Out" | "Unknown";

export interface LiveAttendanceDto {
  employeeId: string;
  name: string;
  status: AttendanceStatus;
  lastCheckIn?: string; // ISO 8601
  lastCheckOut?: string; // ISO 8601
  lastUpdatedAtUtc: string;
}

export interface AttendanceKpiDto {
  total: number;
  present: number;
  late: number;
  absent: number;
}

export type DlqStatus = "Pending" | "Mapped" | "Reprocessing" | "Reprocessed" | "Failed" | "Closed";

export interface DlqPunchDto {
  id: string;
  payload: string;
  biometricId: string;
  deviceId: string;
  timestampUtc: string;
  reason: string;
  status: DlqStatus;
}

export type FraudSeverity = "Low" | "Medium" | "High";
export type FraudStatus = "Detected" | "UnderReview" | "Confirmed" | "Dismissed";

export interface FraudDto {
  id: string;
  employeeId: string;
  employeeName?: string;
  type: string;
  description: string;
  severityScore: number;
  severity: FraudSeverity;
  detectedAtUtc: string;
  status: FraudStatus;
  reviewedBy?: string;
  reviewNotes?: string;
  reviewedAtUtc?: string;
}

export interface ReprocessRequest {
  employeeId?: string;
  fromDate: string;
  toDate: string;
}

export interface ReprocessJobDto {
  jobId: string;
  progress: number;
  status: "Queued" | "Processing" | "Completed" | "Failed";
  message?: string;
}
