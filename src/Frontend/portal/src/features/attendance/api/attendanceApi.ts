import { 
  LiveAttendanceDto, 
  AttendanceKpiDto, 
  DlqPunchDto, 
  FraudDto, 
  ReprocessJobDto, 
  ReprocessRequest 
} from "../types";

const BASE_URL = process.env.NEXT_PUBLIC_API_BASE_URL || "http://localhost:5000";

/**
 * Fetch functions for the Attendance feature.
 */

export const fetchLiveAttendance = async (): Promise<LiveAttendanceDto[]> => {
  const res = await fetch(`${BASE_URL}/api/attendance/live`);
  if (!res.ok) throw new Error("Failed to fetch live attendance");
  return res.json();
};

export const fetchAttendanceKpis = async (): Promise<AttendanceKpiDto> => {
  const res = await fetch(`${BASE_URL}/api/attendance/kpis`);
  if (!res.ok) throw new Error("Failed to fetch KPIs");
  return res.json();
};

export const fetchDlq = async (): Promise<DlqPunchDto[]> => {
  const res = await fetch(`${BASE_URL}/api/attendance/dlq`);
  if (!res.ok) throw new Error("Failed to fetch DLQ");
  return res.json();
};

export const mapDlq = async (id: string, employeeId: string): Promise<{ jobId: string }> => {
  const res = await fetch(`${BASE_URL}/api/attendance/dlq/${id}/map`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ employeeId }),
  });
  if (!res.ok) throw new Error("Failed to map DLQ punch");
  return res.json();
};

export const fetchFraudFlags = async (): Promise<FraudDto[]> => {
  const res = await fetch(`${BASE_URL}/api/attendance/fraud`);
  if (!res.ok) throw new Error("Failed to fetch fraud flags");
  return res.json();
};

export const reviewFraud = async (id: string, status: string, notes: string): Promise<void> => {
  const res = await fetch(`${BASE_URL}/api/attendance/fraud/${id}/review`, {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ status, notes }),
  });
  if (!res.ok) throw new Error("Failed to review fraud flag");
};

export const triggerReprocess = async (request: ReprocessRequest): Promise<ReprocessJobDto> => {
  const res = await fetch(`${BASE_URL}/api/attendance/reprocess`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(request),
  });
  if (!res.ok) throw new Error("Failed to trigger reprocessing");
  return res.json();
};

export const fetchEmployees = async (): Promise<{ id: string; name: string }[]> => {
  const res = await fetch(`${BASE_URL}/api/hr/employees/compact`);
  if (!res.ok) throw new Error("Failed to fetch employees");
  return res.json();
};

export const fetchJobProgress = async (jobId: string): Promise<ReprocessJobDto> => {
  const res = await fetch(`${BASE_URL}/api/attendance/reprocess/${jobId}`);
  if (!res.ok) throw new Error("Failed to fetch job progress");
  return res.json();
};
