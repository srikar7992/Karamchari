namespace Karamchari.Performance.Domain.Reporting;

public enum ExportJobStatus
{
    Queued = 1,
    Processing = 2,
    Ready = 3,
    Failed = 4,
    Cancelled = 5,
}
