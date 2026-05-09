namespace Karamchari.Payroll.Domain.FnF;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public sealed class FnFLineItem
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid Id { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public FnFLineItemType Type { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string Description { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public decimal Amount { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public bool IsDeduction { get; private set; }
    public string? Reference { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public bool IsTaxable { get; private set; }

    private FnFLineItem() { }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public static FnFLineItem Create(
        FnFLineItemType type,
        string description,
        decimal amount,
        bool isDeduction,
        bool isTaxable = false,
        string? reference = null)
    {
        return new FnFLineItem
        {
            Id = Guid.NewGuid(),
            Type = type,
            Description = description,
            Amount = Math.Abs(amount),
            IsDeduction = isDeduction,
            IsTaxable = isTaxable,
            Reference = reference
        };
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public decimal SignedAmount => IsDeduction ? -Amount : Amount;
}
