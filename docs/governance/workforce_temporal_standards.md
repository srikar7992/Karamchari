# Workforce Temporal Standards

## 1. Primary Time Rule
- **Storage:** All database timestamps MUST be `DateTimeOffset` in UTC.
- **Display:** Convert to `TimeZoneId` only at the presentation or report layer.
- **Calculation:** Shift boundaries and overtime MUST be calculated using the `TimeZoneId` of the `WorkforceLocation` or `WorkforceSchedule`.

## 2. Shift Boundary Semantics
- **Business Day:** Defined by the `ShiftDefinition`. A shift starting at 22:00 on Monday and ending at 06:00 on Tuesday belongs to **Monday's** roster.
- **Cross-Midnight:** `AttendanceSession` handles multi-day spans. `WorkDate` is the anchor date of the shift start.

## 3. DST Transition Behavior
- **Spring Forward (23h day):** If a shift overlaps the gap, actual duration is calculated via UTC offset math. 
- **Fall Back (25h day):** Overlap hour is tracked. Total work hours use UTC difference to ensure pay accuracy regardless of local clock repeat.

## 4. Timezone Ownership
- **Location-Bound:** Employees inherit TimeZone from their assigned `WorkforceLocation`.
- **Roster-Bound:** Multi-region rosters MUST define a `TimeZoneId` at the `WorkforceSchedule` level to ensure consistent cutoff windows.
