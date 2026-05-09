using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Performance.Domain.Reporting;

/// <summary>
/// Aggregate representing one async export request.
///
/// State machine: Queued â†’ Processing â†’ Ready | Failed | Cancelled
///
/// Idempotency key: TenantId:ReportType:ParameterHash:RequestedByEmployeeId
/// Duplicates within 60 seconds return the existing job; beyond 60 seconds a new job is valid.
///
/// ADR-0013: exports run async; streaming query â†’ ClosedXML/CSV writer â†’ Azure Blob (24h TTL).
/// </summary>
public sealed class ExportJob : AggregateRoot<Guid>, ITenantOwned
{
    private ExportJob() { /* EF materialization */ }

    private ExportJob(
        Guid id,
        string tenantId,
        Guid requestedByEmployeeId,
        ReportType reportType,
        string parametersJson,
        string idempotencyKey) : base(id)
    {
        TenantId = tenantId;
        RequestedByEmployeeId = requestedByEmployeeId;
        ReportType = reportType;
        ParametersJson = parametersJson;
        IdempotencyKey = idempotencyKey;
        Status = ExportJobStatus.Queued;
        CreatedOnUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string TenantId { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid RequestedByEmployeeId { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public ReportType ReportType { get; private set; }

    /// <summary>JSON-serialized report parameters (filters, cycle IDs, date ranges).</summary>
    public string ParametersJson { get; private set; } = string.Empty;

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string IdempotencyKey { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public ExportJobStatus Status { get; private set; }

    /// <summary>Azure Blob Storage URI set when Status transitions to Ready.</summary>
    public string? BlobUri { get; private set; }

    public string? ErrorMessage { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DateTimeOffset CreatedOnUtc { get; private set; }
    public DateTimeOffset? CompletedOnUtc { get; private set; }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public static ExportJob Create(
        string tenantId,
        Guid requestedByEmployeeId,
        ReportType reportType,
        string parametersJson,
        string idempotencyKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(parametersJson);
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);

        return new ExportJob(Guid.NewGuid(), tenantId, requestedByEmployeeId, reportType, parametersJson, idempotencyKey);
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void MarkProcessing()
    {
        if (Status != ExportJobStatus.Queued)
            throw new InvalidOperationException($"Cannot transition to Processing from {Status}.");

        Status = ExportJobStatus.Processing;
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void MarkReady(string blobUri)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(blobUri);

        if (Status != ExportJobStatus.Processing)
            throw new InvalidOperationException($"Cannot transition to Ready from {Status}.");

        Status = ExportJobStatus.Ready;
        BlobUri = blobUri;
        CompletedOnUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void MarkFailed(string errorMessage)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(errorMessage);

        Status = ExportJobStatus.Failed;
        ErrorMessage = errorMessage;
        CompletedOnUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void Cancel()
    {
        if (Status is ExportJobStatus.Ready or ExportJobStatus.Failed)
            throw new InvalidOperationException($"Cannot cancel job in terminal state {Status}.");

        Status = ExportJobStatus.Cancelled;
        CompletedOnUtc = DateTimeOffset.UtcNow;
    }
}
