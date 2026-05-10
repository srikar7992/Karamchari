using System.Globalization;

namespace Karamchari.Core.Persistence.Tenant;

public sealed class PooledConnectionContaminationException : Exception
{
    public string ExpectedTenantId { get; }
    public string? ActualTenantId { get; }
    public string ConnectionId { get; }
    public DateTime DetectedAt { get; }

    public PooledConnectionContaminationException(string expectedTenantId, string? actualTenantId)
        : this(expectedTenantId, actualTenantId, CreateDefaultMessage(expectedTenantId, actualTenantId))
    {
    }

    public PooledConnectionContaminationException(string expectedTenantId, string? actualTenantId, string message)
        : this(expectedTenantId, actualTenantId, message, null)
    {
    }

    public PooledConnectionContaminationException(
        string expectedTenantId,
        string? actualTenantId,
        string message,
        Exception? innerException)
        : base(message, innerException)
    {
        ExpectedTenantId = expectedTenantId ?? throw new ArgumentNullException(nameof(expectedTenantId));
        ActualTenantId = actualTenantId;
        ConnectionId = GenerateConnectionId();
        DetectedAt = DateTime.UtcNow;
    }

    private static string CreateDefaultMessage(string expectedTenantId, string? actualTenantId)
    {
        return string.Format(
            CultureInfo.InvariantCulture,
            "Pooled connection contamination detected. Expected tenant: '{0}', Actual tenant: '{1}'",
            expectedTenantId,
            actualTenantId ?? "(null)");
    }

    private static string GenerateConnectionId()
    {
        return Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);
    }

    public override string ToString()
    {
        return string.Format(
            CultureInfo.InvariantCulture,
            "PooledConnectionContaminationException: {0}\nExpectedTenantId: {1}\nActualTenantId: {2}\nConnectionId: {3}\nDetectedAt: {4:o}",
            Message,
            ExpectedTenantId,
            ActualTenantId ?? "(null)",
            ConnectionId,
            DetectedAt);
    }

    public TenantContaminationInfo GetContaminationInfo()
    {
        return new TenantContaminationInfo
        {
            ExpectedTenantId = ExpectedTenantId,
            ActualTenantId = ActualTenantId,
            ConnectionId = ConnectionId,
            DetectedAt = DetectedAt,
            Message = Message
        };
    }
}

public sealed class TenantContaminationInfo
{
    public string ExpectedTenantId { get; init; } = string.Empty;
    public string? ActualTenantId { get; init; }
    public string ConnectionId { get; init; } = string.Empty;
    public DateTime DetectedAt { get; init; }
    public string Message { get; init; } = string.Empty;
}
