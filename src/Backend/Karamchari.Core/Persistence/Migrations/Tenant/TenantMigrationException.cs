namespace Karamchari.Core.Persistence.Migrations.Tenant;

public sealed class TenantMigrationException : Exception
{
    public string TenantId { get; }
    public TenantMigrationError Error { get; }
    public string? MigrationId { get; }
    public int? FailedAtStep { get; }

    public TenantMigrationException(string message)
        : this(message, TenantMigrationError.Unknown)
    {
    }

    public TenantMigrationException(string message, TenantMigrationError error)
        : this(message, error, null)
    {
    }

    public TenantMigrationException(string message, TenantMigrationError error, Exception? innerException)
        : this(message, error, null, null, null, innerException)
    {
    }

    public TenantMigrationException(
        string message,
        TenantMigrationError error,
        string? tenantId,
        string? migrationId,
        int? failedAtStep,
        Exception? innerException)
        : base(message, innerException)
    {
        TenantId = tenantId ?? string.Empty;
        Error = error;
        MigrationId = migrationId;
        FailedAtStep = failedAtStep;
    }

    public TenantMigrationException WithTenant(string tenantId)
    {
        return new TenantMigrationException(
            Message,
            Error,
            tenantId,
            MigrationId,
            FailedAtStep,
            InnerException);
    }

    public TenantMigrationException WithMigration(string migrationId)
    {
        return new TenantMigrationException(
            Message,
            Error,
            TenantId,
            migrationId,
            FailedAtStep,
            InnerException);
    }

    public TenantMigrationException WithFailedStep(int step)
    {
        return new TenantMigrationException(
            Message,
            Error,
            TenantId,
            MigrationId,
            step,
            InnerException);
    }

    public override string ToString()
    {
        var details = new List<string>
        {
            base.ToString(),
            $"Error: {Error}"
        };

        if (!string.IsNullOrEmpty(TenantId))
            details.Add($"TenantId: {TenantId}");

        if (!string.IsNullOrEmpty(MigrationId))
            details.Add($"MigrationId: {MigrationId}");

        if (FailedAtStep.HasValue)
            details.Add($"FailedAtStep: {FailedAtStep}");

        return string.Join(Environment.NewLine, details);
    }

    public TenantMigrationErrorDetails GetErrorDetails()
    {
        return new TenantMigrationErrorDetails
        {
            Error = Error,
            TenantId = TenantId,
            MigrationId = MigrationId,
            FailedAtStep = FailedAtStep,
            Message = Message,
            InnerExceptionMessage = InnerException?.Message
        };
    }
}

public enum TenantMigrationError
{
    Unknown,
    LeaseAcquisitionFailed,
    LeaseExpired,
    CheckpointCorrupted,
    CheckpointNotFound,
    MigrationInProgress,
    MigrationFailed,
    RollbackFailed,
    InvalidMigrationSequence,
    CrossTenantViolation,
    VersionIncompatible,
    PartialUpgradeUnrecoverable,
    ConcurrentMigrationConflict,
    InvalidCheckpointData
}

public sealed class TenantMigrationErrorDetails
{
    public TenantMigrationError Error { get; init; }
    public string? TenantId { get; init; }
    public string? MigrationId { get; init; }
    public int? FailedAtStep { get; init; }
    public string? Message { get; init; }
    public string? InnerExceptionMessage { get; init; }
}
