using Karamchari.FinancialOps.Contracts.Reimbursements;

namespace Karamchari.FinancialOps.Domain.Reimbursements;

/// <summary>
/// Represents an operational hold placed on a financial operation to prevent further processing
/// until specific conditions (e.g., audit, compliance review) are met.
/// </summary>
public record FinancialOperationalHold(
    FinancialOperationalHoldType Type,
    string Reason,
    string PlacedBy,
    DateTimeOffset PlacedAt)
{
    /// <summary>
    /// Gets a value indicating whether this hold is still active.
    /// </summary>
    public bool IsActive { get; init; } = true;

    /// <summary>
    /// Gets the timestamp when the hold was released.
    /// </summary>
    public DateTimeOffset? ReleasedAt { get; init; }

    /// <summary>
    /// Gets the identifier of the person who released the hold.
    /// </summary>
    public string? ReleasedBy { get; init; }
}
