using Karamchari.Core.Multitenancy;

namespace Karamchari.TimeAttendance.Domain.IoT;

public enum JobStatus
{
    Queued = 0,
    Processing = 1,
    Completed = 2,
    Failed = 3
}

public sealed class BackgroundJob : ITenantOwned
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string TenantId { get; set; } = string.Empty;

    public string JobType { get; init; } = string.Empty;
    public JobStatus Status { get; set; } = JobStatus.Queued;
    public int ProgressPercent { get; set; }
    public string? ErrorMessage { get; set; }

    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? FinishedAt { get; set; }
}
