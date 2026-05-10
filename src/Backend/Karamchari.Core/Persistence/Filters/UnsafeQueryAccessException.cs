using System.Globalization;

namespace Karamchari.Core.Persistence.Filters;

public sealed class UnsafeQueryAccessException : Exception
{
    public string? AttemptedTenantId { get; }
    public string? QueryDetails { get; }
    public bool IsCrossTenantAttempt { get; }

    public UnsafeQueryAccessException()
    {
    }

    public UnsafeQueryAccessException(string message)
        : base(message)
    {
    }

    public UnsafeQueryAccessException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

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
