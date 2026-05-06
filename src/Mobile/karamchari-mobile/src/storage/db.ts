import * as SQLite from "expo-sqlite";

export type PunchEntity = {
  id: string;
  employeeId: string;
  lat: number;
  lon: number;
  accuracy: number;
  eventTimestamp: string;
  syncStatus: "PENDING" | "SYNCING" | "SYNCED" | "FAILED";
  retryCount: number;
  isOfflineCaptured: number; // 0 or 1
  createdAt: string;
};

let db: SQLite.SQLiteDatabase | null = null;

export const getDb = async () => {
  if (db) return db;
  db = await SQLite.openDatabaseAsync("karamchari_punches.db");
  
  await db.execAsync(`
    PRAGMA journal_mode = WAL;
    CREATE TABLE IF NOT EXISTS punches (
      id TEXT PRIMARY KEY NOT NULL,
      employeeId TEXT NOT NULL,
      lat REAL NOT NULL,
      lon REAL NOT NULL,
      accuracy REAL NOT NULL,
      eventTimestamp TEXT NOT NULL,
      syncStatus TEXT NOT NULL,
      retryCount INTEGER NOT NULL DEFAULT 0,
      isOfflineCaptured INTEGER NOT NULL DEFAULT 0,
      createdAt TEXT NOT NULL
    );
  `);
  
  return db;
};

export const insertPunch = async (punch: PunchEntity) => {
  const database = await getDb();
  await database.runAsync(
    `INSERT INTO punches (id, employeeId, lat, lon, accuracy, eventTimestamp, syncStatus, retryCount, isOfflineCaptured, createdAt) 
     VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?);`,
    [
      punch.id,
      punch.employeeId,
      punch.lat,
      punch.lon,
      punch.accuracy,
      punch.eventTimestamp,
      punch.syncStatus,
      punch.retryCount,
      punch.isOfflineCaptured,
      punch.createdAt,
    ]
  );
};

export const getPendingPunches = async (): Promise<PunchEntity[]> => {
  const database = await getDb();
  return await database.getAllAsync<PunchEntity>(
    "SELECT * FROM punches WHERE syncStatus IN ('PENDING', 'SYNCING') ORDER BY createdAt ASC"
  );
};

export const updatePunchStatus = async (id: string, status: PunchEntity["syncStatus"], retryCount?: number) => {
  const database = await getDb();
  if (retryCount !== undefined) {
    await database.runAsync("UPDATE punches SET syncStatus = ?, retryCount = ? WHERE id = ?", [status, retryCount, id]);
  } else {
    await database.runAsync("UPDATE punches SET syncStatus = ? WHERE id = ?", [status, id]);
  }
};
