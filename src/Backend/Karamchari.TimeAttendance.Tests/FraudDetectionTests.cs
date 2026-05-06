using Karamchari.Core.Multitenancy;
using Karamchari.TimeAttendance.Domain.IoT;
using Karamchari.TimeAttendance.Domain.Shifts;
using Karamchari.TimeAttendance.Persistence;
using Karamchari.TimeAttendance.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Karamchari.TimeAttendance.Tests;

public class FraudDetectionTests
{
    private const string TestTenant = "test";

    private static TimeAttendanceDbContext BuildDbContext()
    {
        var options = new DbContextOptionsBuilder<TimeAttendanceDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var tenantCtx = new TenantContext(TestTenant, TenantSource.JwtClaim);
        var tenantProviderMock = new Mock<ITenantProvider>();
        tenantProviderMock.Setup(p => p.GetTenant()).Returns(tenantCtx);
        TenantContext? outTenant = tenantCtx;
        tenantProviderMock.Setup(p => p.TryGetTenant(out outTenant)).Returns(true);

        return new TimeAttendanceDbContext(options, tenantProviderMock.Object);
    }

    private static RawPunch MakePunch(Guid employeeId, DateTime timestampUtc, PunchType type = PunchType.Unknown) =>
        RawPunch.Create(TestTenant, employeeId, "device-1", Guid.NewGuid().ToString(),
            timestampUtc, timestampUtc, "UTC", timestampUtc, type, PunchSource.Biometric);

    private static ShiftTemplate MakeShift(string name, TimeSpan start, TimeSpan end, bool isNight = false) =>
        ShiftTemplate.Create(TestTenant, name, start, end, graceMinutes: 15, halfDayThreshold: 240, otThreshold: 480, isNightShift: isNight);

    [Fact]
    public async Task ProcessDay_ShouldDeduplicatePunchesWithinOneMinute()
    {
        var dbContext = BuildDbContext();
        var rosteringMock = new Mock<ShiftRosteringEngine>(dbContext);
        var engine = new ShiftReconciliationEngine(dbContext, rosteringMock.Object);

        var employeeId = Guid.NewGuid();
        var date = new DateTime(2026, 5, 5, 0, 0, 0, DateTimeKind.Utc);

        var shift = MakeShift("General", new TimeSpan(9, 0, 0), new TimeSpan(18, 0, 0));
        rosteringMock.Setup(r => r.ResolveShiftAsync(employeeId, It.IsAny<DateTime>(), default))
            .ReturnsAsync(shift);

        // Three punches at 9:00:10, 9:00:20, 9:00:40 (same minute) → treated as one IN
        var p1 = MakePunch(employeeId, date.AddHours(9).AddSeconds(10));
        var p2 = MakePunch(employeeId, date.AddHours(9).AddSeconds(20));
        var p3 = MakePunch(employeeId, date.AddHours(9).AddSeconds(40));
        var pOut = MakePunch(employeeId, date.AddHours(18));

        dbContext.RawPunches.AddRange(p1, p2, p3, pOut);
        await dbContext.SaveChangesAsync();

        await engine.ProcessDayAsync(employeeId, date);

        var result = await dbContext.AttendanceResults
            .FirstOrDefaultAsync(r => r.EmployeeId == employeeId && r.Date == date.Date);

        Assert.NotNull(result);
        Assert.True(result.IsPresent);
        Assert.Equal(540, result.WorkedMinutes);
    }
}
