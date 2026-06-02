# Disaster Recovery Execution Evidence

Date: 2026-06-02
Environment: Local docker (SQL Server 2022 container: local-sqlserver-1)

## F1: Backup → Destroy → Restore → Verify

### Step 1: Backup Taken

Command: `scripts\disaster-recovery\backup.ps1`
Backup file: `backups\20260602-211103\Karamchari_20260602_211103.bak`
Pages: 674 (data: 672, log: 2)
Size: 0.7 MB
Speed: 125 MB/sec
Duration: 0.042 seconds

### Step 2: Database Destroyed

Command: `ALTER DATABASE [Karamchari] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [Karamchari];`
Verification: `SELECT name FROM sys.databases WHERE name='Karamchari'` → 0 rows (confirmed destroyed)

### Step 3: Database Restored

Command: `scripts\disaster-recovery\restore.ps1`
Pages restored: 674 (data: 672, log: 2)
Speed: 154 MB/sec
Duration: 0.034 seconds (1.4 seconds total including docker cp)

### Step 4: Verification

Command: `scripts\disaster-recovery\verify-recovery.ps1`

| Check | Result |
|-------|--------|
| Database 'Karamchari' exists | PASS |
| dbo.OutboxMessage row count | 0 rows (clean state) |
| dbo.OutboxState row count | 0 rows |
| dbo.InboxState row count | 0 rows |
| EF Core migration history | PASS — 8 migrations recorded |
| API health endpoint | PASS — HTTP 200 OK |
| Stale outbox messages | PASS — 0 stale locks |

**Overall: RECOVERY VERIFICATION PASSED**

## F3: Environment Rebuild

Script: `scripts\disaster-recovery\rebuild-environment.ps1` — ready but not executed in this run.
Infrastructure services verified running: sqlserver, redis, rabbitmq, seq, otel-collector, prometheus.

## Summary

RTO (Restore Time Objective): 1.4 seconds for 0.7 MB database.
RPO (Recovery Point Objective): Point-in-time backup; data loss = 0 for this run (backup taken immediately before destroy).
