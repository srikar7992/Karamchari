using System.Globalization;

namespace Karamchari.Core.Persistence.Tenant;

/// <summary>
/// Exception thrown when a pooled database connection is found to have a SESSION_CONTEXT
/// that does not match the current execution tenant, indicating potential cross-tenant leakage.
/// </summary>
public sealed class PooledConnectionContaminationException : Exception
{
    /// <summary>The tenant identifier that was expected based on current execution context.</summary>
    public string ExpectedTenantId { get; }

    /// <summary>The tenant identifier that was actually found in the connection's SESSION_CONTEXT.</summary>
    public string? ActualTenantId { get; }

    /// <summary>A unique identifier for the connection instance where contamination was detected.</summary>
    public string ConnectionId { get; }

    /// <summary>The timestamp when the contamination was detected.</summary>
    public DateTime DetectedAt { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PooledConnectionContaminationException"/> class.
    /// </summary>
    /// <param name="expectedTenantId">The expected tenant identifier.</param>
    /// <param name="actualTenantId">The actual tenant identifier found.</param>
    public PooledConnectionContaminationException(string expectedTenantId, string? actualTenantId)
        : this(expectedTenantId, actualTenantId, CreateDefaultMessage(expectedTenantId, actualTenantId))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PooledConnectionContaminationException"/> class.
    /// </summary>
    /// <param name="expectedTenantId">The expected tenant identifier.</param>
    /// <param name="actualTenantId">The actual tenant identifier found.</param>
    /// <param name="message">The exception message.</param>
    public PooledConnectionContaminationException(string expectedTenantId, string? actualTenantId, string message)
        : this(expectedTenantId, actualTenantId, message, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PooledConnectionContaminationException"/> class.
    /// </summary>
    /// <param name="expectedTenantId">The expected tenant identifier.</param>
    /// <param name="actualTenantId">The actual tenant identifier found.</param>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">The inner exception.</param>
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

    /// <inheritdoc/>
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

    /// <summary>
    /// Gets a structured summary of the contamination for logging or reporting.
    /// </summary>
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

/// <summary>
/// Structured information about a detected tenant context contamination.
/// </summary>
public sealed class TenantContaminationInfo
{
    /// <summary>The expected tenant ID.</summary>
    public string ExpectedTenantId { get; init; } = string.Empty;

    /// <summary>The actual tenant ID found in session context.</summary>
    public string? ActualTenantId { get; init; }

    /// <summary>The unique ID of the contaminated connection.</summary>
    public string ConnectionId { get; init; } = string.Empty;

    /// <summary>When the contamination was detected.</summary>
    public DateTime DetectedAt { get; init; }

    /// <summary>The failure message.</summary>
    public string Message { get; init; } = string.Empty;
}
