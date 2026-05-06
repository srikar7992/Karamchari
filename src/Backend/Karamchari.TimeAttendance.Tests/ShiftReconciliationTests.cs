using Karamchari.Core.Multitenancy;
using Karamchari.TimeAttendance.Domain.IoT;
using Karamchari.TimeAttendance.Domain.Shifts;
using Karamchari.TimeAttendance.Persistence;
using Karamchari.TimeAttendance.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Karamchari.TimeAttendance.Tests;

public class ShiftReconciliationTests
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

    private static RawPunch MakePunch(Guid employeeId, DateTime timestampUtc) =>
        RawPunch.Create(TestTenant, employeeId, "device-1", Guid.NewGuid().ToString(),
            timestampUtc, timestampUtc, "UTC", timestampUtc, PunchType.Unknown, PunchSource.Biometric);

    private static ShiftTemplate MakeShift(string name, TimeSpan start, TimeSpan end, bool isNight = false) =>
        ShiftTemplate.Create(TestTenant, name, start, end, graceMinutes: 15, halfDayThreshold: 240, otThreshold: 480, isNightShift: isNight);

    [Fact]
    public async Task ProcessDay_ShouldPairPunchesIntoSessions()
    {
        var dbContext = BuildDbContext();
        var rosteringMock = new Mock<ShiftRosteringEngine>(dbContext);
        var engine = new ShiftReconciliationEngine(dbContext, rosteringMock.Object);

        var employeeId = Guid.NewGuid();
        var date = new DateTime(2026, 5, 5, 0, 0, 0, DateTimeKind.Utc);

        var shift = MakeShift("General", new TimeSpan(9, 0, 0), new TimeSpan(18, 0, 0));
        rosteringMock.Setup(r => r.ResolveShiftAsync(employeeId, It.IsAny<DateTime>(), default))
            .ReturnsAsync(shift);

        var inPunch = MakePunch(employeeId, date.AddHours(9).AddMinutes(5));
        var outPunch = MakePunch(employeeId, date.AddHours(18).AddMinutes(10));

        dbContext.RawPunches.AddRange(inPunch, outPunch);
        await dbContext.SaveChangesAsync();

        await engine.ProcessDayAsync(employeeId, date);

        var result = await dbContext.AttendanceResults
            .FirstOrDefaultAsync(r => r.EmployeeId == employeeId && r.Date == date.Date);

        Assert.NotNull(result);
        Assert.True(result.IsPresent);
        Assert.True(result.WorkedMinutes > 500);
        Assert.True(result.OvertimeMinutes >= 10);
    }

    [Fact]
    public async Task ProcessDay_ShouldDetectLateMark_WhenGracePeriodExceeded()
    {
        var dbContext = BuildDbContext();
        var rosteringMock = new Mock<ShiftRosteringEngine>(dbContext);
        var engine = new ShiftReconciliationEngine(dbContext, rosteringMock.Object);

        var employeeId = Guid.NewGuid();
        var date = new DateTime(2026, 5, 5, 0, 0, 0, DateTimeKind.Utc);

        var shift = MakeShift("General", new TimeSpan(9, 0, 0), new TimeSpan(18, 0, 0));
        rosteringMock.Setup(r => r.ResolveShiftAsync(employeeId, It.IsAny<DateTime>(), default))
            .ReturnsAsync(shift);

        var inPunch = MakePunch(employeeId, date.AddHours(9).AddMinutes(20));
        var outPunch = MakePunch(employeeId, date.AddHours(18));

        dbContext.RawPunches.AddRange(inPunch, outPunch);
        await dbContext.SaveChangesAsync();

        await engine.ProcessDayAsync(employeeId, date);

        var result = await dbContext.AttendanceResults
            .FirstOrDefaultAsync(r => r.EmployeeId == employeeId && r.Date == date.Date);

        Assert.NotNull(result);
        Assert.True(result.IsLate);
    }

    [Fact]
    public async Task ProcessDay_ShouldHandleNightShiftBoundary()
    {
        var dbContext = BuildDbContext();
        var rosteringMock = new Mock<ShiftRosteringEngine>(dbContext);
        var engine = new ShiftReconciliationEngine(dbContext, rosteringMock.Object);

        var employeeId = Guid.NewGuid();
        var date = new DateTime(2026, 5, 5, 0, 0, 0, DateTimeKind.Utc);

        var shift = MakeShift("Night", new TimeSpan(22, 0, 0), new TimeSpan(6, 0, 0), isNight: true);
        rosteringMock.Setup(r => r.ResolveShiftAsync(employeeId, It.IsAny<DateTime>(), default))
            .ReturnsAsync(shift);

        var inPunch = MakePunch(employeeId, date.AddHours(22).AddMinutes(5));
        var outPunch = MakePunch(employeeId, date.AddDays(1).AddHours(6).AddMinutes(5));

        dbContext.RawPunches.AddRange(inPunch, outPunch);
        await dbContext.SaveChangesAsync();

        await engine.ProcessDayAsync(employeeId, date);

        var result = await dbContext.AttendanceResults
            .FirstOrDefaultAsync(r => r.EmployeeId == employeeId && r.Date == date.Date);

        Assert.NotNull(result);
        Assert.True(result.IsPresent);
        Assert.Equal(480, result.WorkedMinutes);
    }
}
