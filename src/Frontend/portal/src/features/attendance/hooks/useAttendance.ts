import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useEffect } from "react";
import { 
  fetchLiveAttendance, 
  fetchAttendanceKpis, 
  fetchDlq, 
  mapDlq, 
  fetchFraudFlags, 
  reviewFraud 
} from "../api/attendanceApi";
import { attendanceHub } from "@/lib/signalr/AttendanceHubClient";
import { LiveAttendanceDto } from "../types";

/**
 * Hook for managing live attendance state with SignalR O(1) patching.
 */
export const useLiveAttendance = () => {
  const queryClient = useQueryClient();

  const query = useQuery({
    queryKey: ["attendance", "live"],
    queryFn: fetchLiveAttendance,
    // Convert array to map for O(1) access
    select: (data) => {
      const map: Record<string, LiveAttendanceDto> = {};
      data.forEach((emp) => (map[emp.employeeId] = emp));
      return map;
    },
  });

  useEffect(() => {
    attendanceHub.start().catch(console.error);

    attendanceHub.onAttendanceUpdated((update: LiveAttendanceUpdatedEvent) => {
      // Patch the cache O(1)
      queryClient.setQueryData<Record<string, LiveAttendanceDto>>(
        ["attendance", "live"],
        (oldData) => {
          if (!oldData) return oldData;
          const existing = oldData[update.employeeId];
          return {
            ...oldData,
            [update.employeeId]: {
              ...existing,
              ...update,
              name: existing?.name || "Employee", // Preserve or default
              status: update.status as any,
            },
          };
        }
      );

      // Also invalidate KPIs since they likely changed
      queryClient.invalidateQueries({ queryKey: ["attendance", "kpis"] });
    });

    return () => {
      attendanceHub.offAttendanceUpdated();
    };
  }, [queryClient]);

  return query;
};

export const useAttendanceKpis = () => {
  return useQuery({
    queryKey: ["attendance", "kpis"],
    queryFn: fetchAttendanceKpis,
    refetchInterval: 60000, // Background refresh every minute as fallback
  });
};

export const useDlq = () => {
  return useQuery({
    queryKey: ["attendance", "dlq"],
    queryFn: fetchDlq,
  });
};

export const useMapDlq = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, employeeId }: { id: string; employeeId: string }) => mapDlq(id, employeeId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["attendance", "dlq"] });
    },
  });
};

export const useFraudFlags = () => {
  return useQuery({
    queryKey: ["attendance", "fraud"],
    queryFn: fetchFraudFlags,
  });
};

export const useReviewFraud = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, status, notes }: { id: string; status: string; notes: string }) => 
      reviewFraud(id, status, notes),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["attendance", "fraud"] });
    },
  });
};

// Internal interface matching the backend event
interface LiveAttendanceUpdatedEvent {
  tenantId: string;
  employeeId: string;
  isPresent: boolean;
  status: string;
  lastCheckIn?: string;
  lastCheckOut?: string;
  lastUpdatedAtUtc: string;
}
