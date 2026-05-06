import * as TaskManager from "expo-task-manager";
import * as BackgroundFetch from "expo-background-fetch";
import { usePunchStore } from "../store/punchStore";

const SYNC_TASK_NAME = "BACKGROUND_PUNCH_SYNC";

// Define the background task
TaskManager.defineTask(SYNC_TASK_NAME, async () => {
  try {
    const syncQueue = usePunchStore.getState().syncQueue;
    await syncQueue();
    return BackgroundFetch.BackgroundFetchResult.NewData;
  } catch (err) {
    return BackgroundFetch.BackgroundFetchResult.Failed;
  }
});

export const registerBackgroundSync = async () => {
  try {
    await BackgroundFetch.registerTaskAsync(SYNC_TASK_NAME, {
      minimumInterval: 60 * 15, // 15 minutes (minimum allowed by OS)
      stopOnTerminate: false,
      startOnBoot: true,
    });
    console.log("Background sync registered");
  } catch (err) {
    console.error("Background sync registration failed:", err);
  }
};
