using Karamchari.TimeAttendance.Domain.Geofencing;
using Karamchari.TimeAttendance.Domain.IoT;
using Karamchari.TimeAttendance.Persistence;
using Karamchari.TimeAttendance.Services.Fraud;
using Karamchari.TimeAttendance.Services.Geofencing;
using Microsoft.EntityFrameworkCore;
using Moq;
using NetTopologySuite.Geometries;
using Xunit;

namespace Karamchari.TimeAttendance.Tests;

public class SpatialAndFraudTests : IDisposable
{
    private readonly TimeAttendanceDbContext _dbContext;
    private readonly GeofenceService _geofenceService;
    private readonly FraudRiskEngine _fraudEngine;

    public SpatialAndFraudTests()
    {
        var options = new DbContextOptionsBuilder<TimeAttendanceDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new TimeAttendanceDbContext(options, new Mock<Karamchari.Core.Multitenancy.ITenantProvider>().Object);
        _geofenceService = new GeofenceService(_dbContext);
        _fraudEngine = new FraudRiskEngine(_dbContext);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task Geofence_Validation_ShouldEnforceRadiusStrictly()
    {
        // Arrange
        var tenantId = "tenant-1";
        var employeeId = Guid.NewGuid();
        
        // Hyderabad Center
        var lat = 17.3850;
        var lon = 78.4860;
        var boundary = LocationBoundary.Create(tenantId, "HQ", lat, lon, 100);
        _dbContext.LocationBoundaries.Add(boundary);
        
        var assignment = EmployeeLocationAssignment.Create(tenantId, employeeId, boundary.Id);
        _dbContext.EmployeeLocationAssignments.Add(assignment);
        
        await _dbContext.SaveChangesAsync();

        // Act & Assert
        
        // 1. Exactly at center
        var (inside1, _, dist1) = await _geofenceService.CheckGeofenceAsync(employeeId, lat, lon);
        Assert.True(inside1);
        Assert.Equal(0, dist1);

        // 2. 50m away (using approx degree conversion for InMemory test)
        // 1 degree ~ 111,139m. 50m ~ 0.00045 degrees.
        var (inside2, _, _) = await _geofenceService.CheckGeofenceAsync(employeeId, lat + 0.0004, lon);
        Assert.True(inside2);

        // 3. 150m away (~0.00135 degrees)
        var (inside3, _, _) = await _geofenceService.CheckGeofenceAsync(employeeId, lat + 0.0015, lon);
        Assert.False(inside3);
    }

    [Fact]
    public async Task FraudEngine_ShouldDetectVelocityAnomaly()
    {
        // Arrange
        var tenantId = "tenant-1";
        var employeeId = Guid.NewGuid();
        var deviceId = "device-1";

        // Punch 1: Hyderabad at 10:00 AM
        var p1 = RawPunch.Create(tenantId, employeeId, deviceId, "ext-1", 
            DateTime.UtcNow.AddMinutes(-60), DateTime.UtcNow.AddMinutes(-60), "UTC", DateTime.UtcNow, 
            PunchType.In, PunchSource.Mobile, 17.385, 78.486);
        _dbContext.RawPunches.Add(p1);
        await _dbContext.SaveChangesAsync();

        // Punch 2: Bangalore at 10:10 AM (Impossible travel: ~570km in 10 mins)
        var p2 = RawPunch.Create(tenantId, employeeId, deviceId, "ext-2", 
            DateTime.UtcNow.AddMinutes(-50), DateTime.UtcNow.AddMinutes(-50), "UTC", DateTime.UtcNow, 
            PunchType.In, PunchSource.Mobile, 12.971, 77.594);
        _dbContext.RawPunches.Add(p2);
        await _dbContext.SaveChangesAsync();

        // Act
        var flag = await _fraudEngine.EvaluatePunchAsync(p2, 10);

        // Assert
        Assert.Equal("Fraud", flag.Severity);
        Assert.Contains("VelocityAnomaly", flag.Explanation);
    }

    [Fact]
    public async Task FraudEngine_ShouldDetectSuddenJump()
    {
        // Arrange
        var tenantId = "tenant-1";
        var employeeId = Guid.NewGuid();
        
        // Punch 1
        var p1 = RawPunch.Create(tenantId, employeeId, "dev-1", "e1", 
            DateTime.UtcNow.AddSeconds(-120), DateTime.UtcNow.AddSeconds(-120), "UTC", DateTime.UtcNow, 
            PunchType.In, PunchSource.Mobile, 17.385, 78.486);
        _dbContext.RawPunches.Add(p1);
        await _dbContext.SaveChangesAsync();

        // Punch 2: 6km jump in 10 seconds
        var p2 = RawPunch.Create(tenantId, employeeId, "dev-1", "e2", 
            DateTime.UtcNow.AddSeconds(-110), DateTime.UtcNow.AddSeconds(-110), "UTC", DateTime.UtcNow, 
            PunchType.In, PunchSource.Mobile, 17.435, 78.486);
        _dbContext.RawPunches.Add(p2);
        await _dbContext.SaveChangesAsync();

        // Act
        var flag = await _fraudEngine.EvaluatePunchAsync(p2, 10);

        // Assert
        Assert.Equal("Fraud", flag.Severity);
        Assert.Contains("SuddenJump", flag.Explanation);
    }

    [Fact]
    public async Task FraudEngine_ShouldDetectDeviceSwitching()
    {
        // Arrange
        var tenantId = "tenant-1";
        var employeeId = Guid.NewGuid();
        
        // Device A
        var p1 = RawPunch.Create(tenantId, employeeId, "Device-A", "e1", 
            DateTime.UtcNow.AddMinutes(-5), DateTime.UtcNow.AddMinutes(-5), "UTC", DateTime.UtcNow, 
            PunchType.In, PunchSource.Mobile, 17.385, 78.486);
        _dbContext.RawPunches.Add(p1);
        await _dbContext.SaveChangesAsync();

        // Device B within 5 mins
        var p2 = RawPunch.Create(tenantId, employeeId, "Device-B", "e2", 
            DateTime.UtcNow, DateTime.UtcNow, "UTC", DateTime.UtcNow, 
            PunchType.In, PunchSource.Mobile, 17.385, 78.486);
        _dbContext.RawPunches.Add(p2);
        await _dbContext.SaveChangesAsync();

        // Act
        var flag = await _fraudEngine.EvaluatePunchAsync(p2, 10);

        // Assert
        Assert.Contains("DeviceSwitch", flag.Explanation);
    }

    [Fact]
    public async Task FraudEngine_ShouldPersistAuditTrail()
    {
        // Arrange
        var tenantId = "tenant-1";
        var employeeId = Guid.NewGuid();
        var p = RawPunch.Create(tenantId, employeeId, "dev", "ext", DateTime.UtcNow, DateTime.UtcNow, "UTC", DateTime.UtcNow, PunchType.In, PunchSource.Mobile, 17.385, 78.486);
        _dbContext.RawPunches.Add(p);
        await _dbContext.SaveChangesAsync();

        // Act
        await _fraudEngine.EvaluatePunchAsync(p, 500); // Trigger suspicious accuracy

        // Assert
        var signal = await _dbContext.FraudSignals.FirstOrDefaultAsync(s => s.EmployeeId == employeeId);
        var flag = await _dbContext.DetailedFraudFlags.FirstOrDefaultAsync(f => f.RawPunchId == p.Id);

        Assert.NotNull(signal);
        Assert.NotNull(flag);
        Assert.Equal("Suspicious", flag.Severity);
    }
}
