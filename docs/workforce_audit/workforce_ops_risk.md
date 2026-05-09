# Workforce Operations Risk Report

## 1. High Dependencies
- **HR Sync:** Attendance relies on valid Employee aggregate. Inactive status in HR must immediately terminate active shifts.
- **Payroll Linkage:** Overtime approved here must flow to Payroll batch. Mismatch leads to financial disputes.

## 2. Technical Risks
- **Clock Drift:** Client-side timestamps are unreliable. Server-side UTC must be authority.
- **Offline Sync:** Mobile workers in remote locations (mines/ports) need offline check-in. Conflict resolution risk during sync.
- **DST Transitions:** Cross-midnight shifts during Daylight Saving Time shifts (Spring/Fall) can cause 23/25 hour workday errors.

## 3. Operational Risks
- **Proxy Punching:** Identity theft via mobile app or QR. Need device fingerprinting.
- **Roster Gaps:** No-show detection depends on "Expected vs Actual" join. High performance load for real-time alerts.
