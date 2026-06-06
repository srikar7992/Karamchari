namespace Karamchari.Compliance.Domain.Retention;

public enum RetentionActionStatus
{
    Scheduled,
    Executing,
    Completed,
    Skipped,     // legal hold prevented execution
    Failed
}
