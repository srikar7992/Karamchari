import { create } from "zustand";
import { PunchEntity, getPendingPunches, updatePunchStatus, insertPunch } from "../storage/db";
import { sendPunch } from "../services/api";
import NetInfo from "@react-native-community/netinfo";

interface PunchState {
  isSyncing: boolean;
  lastSyncError: string | null;
  pendingCount: number;
  syncQueue: () => Promise<void>;
  addPunch: (punch: Omit<PunchEntity, "syncStatus" | "retryCount" | "createdAt" | "isOfflineCaptured">) => Promise<void>;
  refreshPendingCount: () => Promise<void>;
}

export const usePunchStore = create<PunchState>((set, get) => ({
  isSyncing: false,
  lastSyncError: null,
  pendingCount: 0,

  refreshPendingCount: async () => {
    const pending = await getPendingPunches();
    set({ pendingCount: pending.length });
  },

  addPunch: async (punchData) => {
    const isOnline = (await NetInfo.fetch()).isConnected;
    
    const newPunch: PunchEntity = {
      ...punchData,
      syncStatus: "PENDING",
      retryCount: 0,
      isOfflineCaptured: isOnline ? 0 : 1,
      createdAt: new Date().toISOString(),
    };

    await insertPunch(newPunch);
    await get().refreshPendingCount();
    
    // Trigger immediate sync attempt
    get().syncQueue();
  },

  syncQueue: async () => {
    const { isSyncing } = get();
    if (isSyncing) return;

    const isOnline = (await NetInfo.fetch()).isConnected;
    if (!isOnline) return;

    set({ isSyncing: true, lastSyncError: null });

    try {
      const pending = await getPendingPunches();
      
      for (const punch of pending) {
        try {
          await updatePunchStatus(punch.id, "SYNCING");

          await sendPunch({
            employeeId: punch.employeeId,
            timestamp: punch.eventTimestamp,
            lat: punch.lat,
            lon: punch.lon,
            accuracy: punch.accuracy,
            isOfflineCaptured: punch.isOfflineCaptured === 1,
            clientId: punch.id, // Idempotency Key
          });

          await updatePunchStatus(punch.id, "SYNCED");
        } catch (err: any) {
          const newRetryCount = punch.retryCount + 1;
          const newStatus = newRetryCount >= 5 ? "FAILED" : "PENDING";
          
          await updatePunchStatus(punch.id, newStatus, newRetryCount);
          
          set({ lastSyncError: `Failed to sync ${punch.id}: ${err.message}` });
          // Stop processing queue on error to avoid repeated failures in a row
          break;
        }
      }
    } finally {
      set({ isSyncing: false });
      await get().refreshPendingCount();
    }
  },
}));
