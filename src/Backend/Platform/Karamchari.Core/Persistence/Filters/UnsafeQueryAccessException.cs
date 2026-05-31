using System.Globalization;

namespace Karamchari.Core.Persistence.Filters;

/// <summary>
/// Exception thrown when a database query is blocked because it violates tenant isolation rules,
/// contains unsafe SQL patterns, or attempts to bypass global query filters.
/// </summary>
public sealed class UnsafeQueryAccessException : Exception
{
    /// <summary>Gets the tenant ID that was associated with the blocked attempt.</summary>
    public string? AttemptedTenantId { get; }

    /// <summary>Gets diagnostic details about the blocked query.</summary>
    public string? QueryDetails { get; }

    /// <summary>Gets a value indicating whether this was a confirmed cross-tenant access attempt.</summary>
    public bool IsCrossTenantAttempt { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnsafeQueryAccessException"/> class.
    /// </summary>
    public UnsafeQueryAccessException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnsafeQueryAccessException"/> class.
    /// </summary>
    public UnsafeQueryAccessException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnsafeQueryAccessException"/> class.
    /// </summary>
    public UnsafeQueryAccessException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnsafeQueryAccessException"/> class with detailed metadata.
    /// </summary>
    public UnsafeQueryAccessException(
        string message,
        string? attemptedTenantId,
        string? queryDetails,
        bool isCrossTenantAttempt = false)
        : base(message)
    {
        AttemptedTenantId = attemptedTenantId;
        QueryDetails = queryDetails;
        IsCrossTenantAttempt = isCrossTenantAttempt;
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        var details = new List<string>
        {
            base.ToString()
        };

        if (!string.IsNullOrEmpty(AttemptedTenantId))
        {
            details.Add(string.Format(CultureInfo.InvariantCulture, "AttemptedTenantId: {0}", AttemptedTenantId));
        }

        if (!string.IsNullOrEmpty(QueryDetails))
        {
            details.Add(string.Format(CultureInfo.InvariantCulture, "QueryDetails: {0}", QueryDetails));
        }

        details.Add(string.Format(CultureInfo.InvariantCulture, "IsCrossTenantAttempt: {0}", IsCrossTenantAttempt));

        return string.Join(Environment.NewLine, details);
    }
}
