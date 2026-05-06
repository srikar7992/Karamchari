using Karamchari.TimeAttendance.Domain.Geofencing;
using Karamchari.TimeAttendance.Persistence;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace Karamchari.TimeAttendance.Services.Geofencing;

public interface IGeofenceService
{
    Task<(bool isInside, LocationBoundary? boundary, double distance)> CheckGeofenceAsync(Guid employeeId, double latitude, double longitude);
}

public sealed class GeofenceService : IGeofenceService
{
    private readonly TimeAttendanceDbContext _dbContext;

    public GeofenceService(TimeAttendanceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<(bool isInside, LocationBoundary? boundary, double distance)> CheckGeofenceAsync(Guid employeeId, double latitude, double longitude)
    {
        var userPoint = new Point(longitude, latitude) { SRID = 4326 };

        // Fetch assigned boundaries for the employee
        var assignedBoundaryIds = await _dbContext.EmployeeLocationAssignments
            .Where(a => a.EmployeeId == employeeId)
            .Select(a => a.LocationBoundaryId)
            .ToListAsync();

        if (assignedBoundaryIds.Count == 0)
        {
            // If no assignments, we might allow or reject based on policy. 
            // For now, return false as no boundary is defined.
            return (false, null, double.MaxValue);
        }

        var boundaries = await _dbContext.LocationBoundaries
            .Where(b => assignedBoundaryIds.Contains(b.Id) && b.IsActive)
            .ToListAsync();

        LocationBoundary? bestBoundary = null;
        double minDistance = double.MaxValue;

        foreach (var boundary in boundaries)
        {
            // Calculate distance in degrees
            var distanceDegrees = boundary.Location.Distance(userPoint);
            
            // Convert to meters (Approximate: 1 degree ≈ 111,139 meters)
            // Note: In production SQL Server geography, STDistance returns meters directly.
            // This multiplier ensures consistency for InMemory/NTS-only environments.
            var distanceMeters = distanceDegrees * 111_139;
            
            if (distanceMeters < minDistance)
            {
                minDistance = distanceMeters;
                bestBoundary = boundary;
            }
        }

        bool isInside = bestBoundary != null && minDistance <= bestBoundary.RadiusInMeters;

        return (isInside, bestBoundary, minDistance);
    }
}
