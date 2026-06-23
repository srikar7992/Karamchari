import { api } from '@/lib/api/client';
import {
  LiveAttendanceDto,
  AttendanceKpiDto,
  DlqPunchDto,
  FraudDto,
  ReprocessJobDto,
  ReprocessRequest,
} from '../types';

interface LiveSessionDto {
  id: string;
  employeeId: string;
  workDate: string;
  status: string;
  checkInTime: string | null;
  totalWorkHours: number;
}

export const fetchLiveAttendance = async (): Promise<LiveAttendanceDto[]> => {
  const sessions = await api.get<LiveSessionDto[]>('/api/v1/workforce/attendance/sessions/live');
  return sessions.map((s) => ({
    employeeId: s.employeeId,
    name: s.employeeId,
    status: (s.status === 'Late' ? 'Late' : 'Present') as LiveAttendanceDto['status'],
    lastCheckIn: s.checkInTime ? `${s.workDate}T${s.checkInTime}` : undefined,
    lastCheckOut: undefined,
    lastUpdatedAtUtc: new Date().toISOString(),
  }));
};

// GET /api/v1/workforce/attendance/dashboard/health returns status-grouped counts.
// Map to flat KpiDto shape the UI expects.
export const fetchAttendanceKpis = async (): Promise<AttendanceKpiDto> => {
  const raw = await api.get<{
    attendanceSummary: { status: string; count: number }[];
  }>('/api/v1/workforce/attendance/dashboard/health');

  const get = (status: string) =>
    raw.attendanceSummary.find(s => s.status.toLowerCase() === status.toLowerCase())?.count ?? 0;

  const present = get('Present');
  const late = get('Late');
  const absent = get('Absent');
  const onLeave = get('OnLeave');
  const halfDay = get('HalfDay');

  return {
    total: present + late + absent + onLeave + halfDay,
    present,
    late,
    absent,
  };
};

// TODO Phase 2: DLQ/Fraud/Reprocess BFF endpoints not yet implemented.
// Functions retained so callers compile; will 404 until Phase 2 endpoints exist.

export const fetchDlq = (): Promise<DlqPunchDto[]> =>
  api.get<DlqPunchDto[]>('/api/v1/attendance/dlq');

export const mapDlq = (id: string, employeeId: string): Promise<{ jobId: string }> =>
  api.post<{ jobId: string }>(`/api/v1/attendance/dlq/${id}/map`, {
    body: { employeeId },
  });

export const fetchFraudFlags = (): Promise<FraudDto[]> =>
  api.get<FraudDto[]>('/api/v1/attendance/fraud');

export const reviewFraud = (id: string, status: string, notes: string): Promise<void> =>
  api.put<void>(`/api/v1/attendance/fraud/${id}/review`, {
    body: { status, notes },
  });

export const triggerReprocess = (request: ReprocessRequest): Promise<ReprocessJobDto> =>
  api.post<ReprocessJobDto>('/api/v1/attendance/reprocess', { body: request });

export const fetchEmployees = (): Promise<{ id: string; name: string }[]> =>
  api.get<{ id: string; name: string }[]>('/api/v1/employees/compact');

export const fetchJobProgress = (jobId: string): Promise<ReprocessJobDto> =>
  api.get<ReprocessJobDto>(`/api/v1/attendance/reprocess/${jobId}`);
