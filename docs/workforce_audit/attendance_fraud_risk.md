# Attendance Fraud Risk Report

## 1. Geo-Spoofing
- Mobile GPS spoofing common in field workforce. 
- Mitigation: Root/Jailbreak detection + signal accuracy validation.

## 2. Buddy Punching
- Teammate scans QR/Code for absent employee.
- Mitigation: Device fingerprinting (single active device per employee) + biometric readiness.

## 3. Timestamp Manipulation
- Users changing system clock before offline sync.
- Mitigation: Monotonic clock checks + network-provided time validation.

## 4. Location Drift
- Punching in from home via remote access.
- Mitigation: Strict geo-fencing + network IP validation.
