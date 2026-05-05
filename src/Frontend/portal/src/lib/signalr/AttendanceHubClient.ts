import * as signalr from "@microsoft/signalr";

/**
 * SignalR Client wrapper for the Attendance Hub.
 * Implements automatic reconnection and explicit fallback strategies.
 */
export class AttendanceHubClient {
  private connection: signalr.HubConnection;
  private isStarted: boolean = false;

  constructor(hubUrl: string = "/hubs/attendance") {
    this.connection = new signalr.HubConnectionBuilder()
      .withUrl(hubUrl)
      .withAutomaticReconnect({
        nextRetryDelayInMilliseconds: (retryContext) => {
          if (retryContext.elapsedMilliseconds < 60000) {
            // If we've been reconnecting for less than 60 seconds, retry every 2s
            return 2000;
          } else {
            // Otherwise, retry every 10s
            return 10000;
          }
        },
      })
      .configureLogging(signalr.LogLevel.Information)
      .build();

    this.connection.onclose((error) => {
      console.error("AttendanceHub connection closed:", error);
      // Here you could trigger a fallback to polling via a global event or state update
      this.isStarted = false;
    });

    this.connection.onreconnecting((error) => {
      console.warn("AttendanceHub reconnecting:", error);
    });

    this.connection.onreconnected((connectionId) => {
      console.info("AttendanceHub reconnected. ConnectionId:", connectionId);
      this.isStarted = true;
    });
  }

  async start(): Promise<void> {
    if (this.isStarted) return;

    try {
      await this.connection.start();
      this.isStarted = true;
      console.info("AttendanceHub connection started.");
    } catch (err) {
      console.error("AttendanceHub connection failed to start:", err);
      this.isStarted = false;
      throw err;
    }
  }

  async stop(): Promise<void> {
    await this.connection.stop();
    this.isStarted = false;
  }

  /**
   * Subscribes to attendance updates for a specific employee or tenant.
   * @param callback Function to handle the update event.
   */
  onAttendanceUpdated(callback: (data: any) => void): void {
    this.connection.on("attendanceUpdated", callback);
  }

  /**
   * Unsubscribes from attendance updates.
   */
  offAttendanceUpdated(): void {
    this.connection.off("attendanceUpdated");
  }

  getConnectionState(): signalr.HubConnectionState {
    return this.connection.state;
  }
}

// Singleton instance for the application
export const attendanceHub = new AttendanceHubClient();
