namespace Karamchari.Intelligence.Contracts;

public sealed record StaleIntelligenceAlertEvent(
    Guid SignalId,
    string TenantId,
    string SignalType,
    string SubjectId,
    DateTimeOffset LastGeneratedAtUtc,
    string WarningMessage);
