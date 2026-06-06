using Karamchari.Core.Domain.Primitives;

namespace Karamchari.Compliance.Domain.LegalHold;

/// <summary>
/// Defines the data scope covered by a legal hold.
/// Example: Employee=123, Site=Hyderabad, PayrollPeriod=FY2025.
/// </summary>
public sealed class LegalHoldScope : Entity<Guid>
{
    public Guid LegalHoldId { get; private set; }
    public LegalHoldScopeType ScopeType { get; private set; }
    public string ScopeValue { get; private set; } = string.Empty;

    private LegalHoldScope() { }

    public static LegalHoldScope Create(Guid legalHoldId, LegalHoldScopeType scopeType, string scopeValue)
    {
        return new LegalHoldScope
        {
            Id = Guid.NewGuid(),
            LegalHoldId = legalHoldId,
            ScopeType = scopeType,
            ScopeValue = scopeValue
        };
    }
}
