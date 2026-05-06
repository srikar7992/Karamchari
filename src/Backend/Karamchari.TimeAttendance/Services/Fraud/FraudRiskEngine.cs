using Karamchari.TimeAttendance.Domain.Fraud;
using Karamchari.TimeAttendance.Domain.IoT;
using FraudFlag = Karamchari.TimeAttendance.Domain.Fraud.FraudFlag;
using Karamchari.TimeAttendance.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.TimeAttendance.Services.Fraud;

public interface IFraudRiskEngine
{
    Task<FraudFlag> EvaluatePunchAsync(RawPunch currentPunch, double accuracy);
}

public sealed class FraudRiskEngine : IFraudRiskEngine
{
    private readonly TimeAttendanceDbContext _dbContext;

    public FraudRiskEngine(TimeAttendanceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<FraudFlag> EvaluatePunchAsync(RawPunch currentPunch, double accuracy)
    {
        ArgumentNullException.ThrowIfNull(currentPunch);
        var signals = new List<FraudSignal>();
        var tenantId = currentPunch.TenantId;
        var employeeId = currentPunch.EmployeeId;

        // 1. Accuracy Check
        if (accuracy > 100)
        {
            signals.Add(FraudSignal.Create(tenantId, employeeId, "LowAccuracy", 20, $"GPS accuracy is poor: {accuracy}m"));
        }

        // 2. Velocity & Jump Checks (against last punch)
        var lastPunch = await _dbContext.RawPunches
            .Where(p => p.EmployeeId == employeeId && p.Id != currentPunch.Id)
            .OrderByDescending(p => p.TimestampUtc)
            .FirstOrDefaultAsync();

        if (lastPunch != null && lastPunch.Latitude.HasValue && lastPunch.Longitude.HasValue && currentPunch.Latitude.HasValue && currentPunch.Longitude.HasValue)
        {
            var distance = Haversine(lastPunch.Latitude.Value, lastPunch.Longitude.Value, currentPunch.Latitude.Value, currentPunch.Longitude.Value);
            var timeDiffSeconds = (currentPunch.TimestampUtc - lastPunch.TimestampUtc).TotalSeconds;

            if (timeDiffSeconds > 0)
            {
                var velocityKmh = (distance / 1000.0) / (timeDiffSeconds / 3600.0);

                // Speed anomaly (e.g., > 300 km/h is highly suspicious for ground travel)
                if (velocityKmh > 300)
                {
                    signals.Add(FraudSignal.Create(tenantId, employeeId, "VelocityAnomaly", 60, $"Impossible speed detected: {velocityKmh:F1} km/h"));
                }

                // Sudden jump (e.g., > 5km in < 2 mins)
                if (distance > 5000 && timeDiffSeconds < 120)
                {
                    signals.Add(FraudSignal.Create(tenantId, employeeId, "SuddenJump", 80, $"Suspicious location jump: {distance:F0}m in {timeDiffSeconds:F0}s"));
                }
            }
        }

        // 3. Device Switching
        var recentPunchesWithDifferentDevice = await _dbContext.RawPunches
            .Where(p => p.EmployeeId == employeeId && p.DeviceId != currentPunch.DeviceId && p.TimestampUtc > DateTime.UtcNow.AddHours(-1))
            .AnyAsync();

        if (recentPunchesWithDifferentDevice)
        {
            signals.Add(FraudSignal.Create(tenantId, employeeId, "DeviceSwitch", 40, "Employee switched devices within the last hour."));
        }

        // Calculate total score
        double totalScore = signals.Sum(s => s.Score);
        string severity = totalScore >= 70 ? "Fraud" : (totalScore >= 30 ? "Suspicious" : "Safe");
        string explanation = signals.Count > 0 
            ? string.Join(" | ", signals.Select(s => s.Reason)) 
            : "No anomalies detected.";

        // Persist signals
        _dbContext.FraudSignals.AddRange(signals);
        
        var flag = FraudFlag.Create(tenantId, currentPunch.Id, severity, totalScore, explanation);
        _dbContext.DetailedFraudFlags.Add(flag);

        await _dbContext.SaveChangesAsync();

        return flag;
    }

    private static double Haversine(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371000; // Earth radius in meters
        var dLat = ToRad(lat2 - lat1);
        var dLon = ToRad(lon2 - lon1);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }

    private static double ToRad(double degrees) => degrees * (Math.PI / 180.0);
}
