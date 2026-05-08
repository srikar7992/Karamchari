using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Compensation.Domain;

/// <summary>
/// Merit matrix: maps (performance rating, compa-ratio range) → recommended increment percentage.
/// Guides merit cycle allocations. One matrix per review cycle (year).
///
/// Example: ExceedsExpectations + Compa-ratio 80–100% → 8–12% increment.
/// Managers can deviate with HR approval; the matrix is a recommendation, not a hard constraint.
/// </summary>
public sealed class MeritMatrix : AggregateRoot<Guid>, ITenantOwned
{
    private readonly List<MeritMatrixCell> _cells = [];
    private MeritMatrix() { /* EF materialization */ }

    private MeritMatrix(
        Guid id,
        string tenantId,
        string name,
        int reviewYear,
        string currencyCode) : base(id)
    {
        TenantId = tenantId;
        Name = name;
        ReviewYear = reviewYear;
        CurrencyCode = currencyCode;
        IsPublished = false;
    }

    public string TenantId { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public int ReviewYear { get; private set; }
    public string CurrencyCode { get; private set; } = string.Empty;
    public bool IsPublished { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public IReadOnlyCollection<MeritMatrixCell> Cells => _cells.AsReadOnly();

    public static MeritMatrix Create(string tenantId, string name, int reviewYear, string currencyCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(currencyCode);
        if (reviewYear < 2020 || reviewYear > 2100)
            throw new ArgumentOutOfRangeException(nameof(reviewYear));

        return new MeritMatrix(Guid.NewGuid(), tenantId, name.Trim(), reviewYear, currencyCode.ToUpperInvariant());
    }

    public void AddCell(
        MeritRating rating,
        decimal compaRatioMin,
        decimal compaRatioMax,
        decimal minIncrementPercent,
        decimal midIncrementPercent,
        decimal maxIncrementPercent)
    {
        if (IsPublished)
            throw new InvalidOperationException("Cannot modify a published merit matrix.");
        if (compaRatioMin >= compaRatioMax)
            throw new ArgumentException("CompaRatioMin must be less than CompaRatioMax.");

        var cell = new MeritMatrixCell(
            Guid.NewGuid(), Id, rating,
            compaRatioMin, compaRatioMax,
            minIncrementPercent, midIncrementPercent, maxIncrementPercent);
        _cells.Add(cell);
    }

    public void Publish()
    {
        if (_cells.Count == 0)
            throw new InvalidOperationException("Cannot publish an empty merit matrix.");
        IsPublished = true;
    }

    /// <summary>
    /// Looks up recommended increment range for a given employee's rating and compa-ratio.
    /// Returns null if no matching cell (matrix is incomplete or employee is out-of-band).
    /// </summary>
    public MeritMatrixCell? Lookup(MeritRating rating, decimal compaRatio) =>
        _cells.FirstOrDefault(c =>
            c.Rating == rating &&
            compaRatio >= c.CompaRatioMin &&
            compaRatio < c.CompaRatioMax);
}

/// <summary>One cell in the merit matrix grid.</summary>
public sealed class MeritMatrixCell : Entity<Guid>
{
    internal MeritMatrixCell(
        Guid id,
        Guid matrixId,
        MeritRating rating,
        decimal compaRatioMin,
        decimal compaRatioMax,
        decimal minIncrementPercent,
        decimal midIncrementPercent,
        decimal maxIncrementPercent) : base(id)
    {
        MatrixId = matrixId;
        Rating = rating;
        CompaRatioMin = compaRatioMin;
        CompaRatioMax = compaRatioMax;
        MinIncrementPercent = minIncrementPercent;
        MidIncrementPercent = midIncrementPercent;
        MaxIncrementPercent = maxIncrementPercent;
    }

    private MeritMatrixCell() { /* EF materialization */ }

    public Guid MatrixId { get; private set; }
    public MeritRating Rating { get; private set; }
    public decimal CompaRatioMin { get; private set; }
    public decimal CompaRatioMax { get; private set; }
    public decimal MinIncrementPercent { get; private set; }
    public decimal MidIncrementPercent { get; private set; }
    public decimal MaxIncrementPercent { get; private set; }
}
