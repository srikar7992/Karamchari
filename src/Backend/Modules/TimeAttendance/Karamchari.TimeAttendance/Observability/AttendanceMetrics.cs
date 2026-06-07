using System.Diagnostics.Metrics;
using System.Reflection;

namespace Karamchari.TimeAttendance.Observability;

/// <summary>
/// System.Diagnostics.Metrics instruments for the attendance pipeline.
/// Hooks automatically into OpenTelemetry / Application Insights when the host
/// configures a MeterProvider — no extra packages required in this module.
/// </summary>
public sealed class AttendanceMetrics : IDisposable
{
    public static readonly string MeterName = "Karamchari.TimeAttendance";

    private readonly Meter _meter;

    /// <summary>End-to-end latency for a single punch (check-in or check-out), in milliseconds.</summary>
    public Histogram<long> PunchLatencyMs { get; }

    /// <summary>Duration of FinalizeShiftsForDateAsync for one tenant+date, in milliseconds.</summary>
    public Histogram<long> FinalizationLatencyMs { get; }

    /// <summary>Number of times ShiftResolver returned null due to no cache entries (cache miss).</summary>
    public Counter<long> ShiftResolverCacheMisses { get; }

    /// <summary>Number of AttendanceSessions created (walk-ins + scheduled).</summary>
    public Counter<long> AttendanceSessionsCreated { get; }

    /// <summary>Number of regularizations applied.</summary>
    public Counter<long> RegularizationsApproved { get; }

    /// <summary>Number of finalization failures (unhandled exceptions in FinalizeShiftsForDateAsync).</summary>
    public Counter<long> FinalizationFailures { get; }

    public AttendanceMetrics()
    {
        string version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0";
        _meter = new Meter(MeterName, version);

        PunchLatencyMs = _meter.CreateHistogram<long>(
            "attendance.punch.duration",
            unit: "ms",
            description: "End-to-end latency of a single punch (check-in or check-out).");

        FinalizationLatencyMs = _meter.CreateHistogram<long>(
            "attendance.finalization.duration",
            unit: "ms",
            description: "Duration of shift finalization for one tenant+date.");

        ShiftResolverCacheMisses = _meter.CreateCounter<long>(
            "attendance.shift_resolver.cache_misses",
            description: "Cache misses in ShiftResolver — no assignment found for employee+date.");

        AttendanceSessionsCreated = _meter.CreateCounter<long>(
            "attendance.sessions.created",
            description: "AttendanceSessions created (new sessions on check-in).");

        RegularizationsApproved = _meter.CreateCounter<long>(
            "attendance.regularizations.approved",
            description: "Regularizations applied to attendance records.");

        FinalizationFailures = _meter.CreateCounter<long>(
            "attendance.finalization.failures",
            description: "Unhandled exceptions in FinalizeShiftsForDateAsync.");
    }

    public void Dispose() => _meter.Dispose();
}
