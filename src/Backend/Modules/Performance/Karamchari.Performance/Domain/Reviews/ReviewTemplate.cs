using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Performance.Domain.Reviews;

/// <summary>
/// Defines the structure (sections + questions) used for one or more review cycles.
/// Templates are versioned and immutable after first use â€” clone via CloneForEdit().
/// Section weights must sum to 1.0 within a template.
/// </summary>
public sealed class ReviewTemplate : AggregateRoot<Guid>, ITenantOwned
{
    private readonly List<ReviewSection> _sections = [];

    private ReviewTemplate() { /* EF materialization */ }

    private ReviewTemplate(
        Guid id,
        string tenantId,
        string name,
        ReviewCycleType applicableCycleType,
        bool includesSelfReview,
        bool includesManagerReview,
        bool includesPeerReview) : base(id)
    {
        TenantId = tenantId;
        Name = name;
        ApplicableCycleType = applicableCycleType;
        IncludesSelfReview = includesSelfReview;
        IncludesManagerReview = includesManagerReview;
        IncludesPeerReview = includesPeerReview;
        IsActive = true;
        IsLocked = false;
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string TenantId { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string Name { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public ReviewCycleType ApplicableCycleType { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public bool IncludesSelfReview { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public bool IncludesManagerReview { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public bool IncludesPeerReview { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>Locked once a ReviewCycle references this template. No structural changes permitted after lock.</summary>
    public bool IsLocked { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public byte[] RowVersion { get; private set; } = [];
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public IReadOnlyList<ReviewSection> Sections => _sections.AsReadOnly();

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public static ReviewTemplate Create(
        string tenantId,
        string name,
        ReviewCycleType applicableCycleType,
        bool includesSelfReview = true,
        bool includesManagerReview = true,
        bool includesPeerReview = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new ReviewTemplate(Guid.NewGuid(), tenantId, name.Trim(), applicableCycleType,
            includesSelfReview, includesManagerReview, includesPeerReview);
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void AddSection(ReviewSection section)
    {
        EnsureNotLocked();
        ArgumentNullException.ThrowIfNull(section);
        _sections.Add(section);
        ValidateSectionWeights();
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void Lock()
    {
        if (_sections.Count == 0)
            throw new InvalidOperationException("Cannot lock template with no sections.");
        ValidateSectionWeights();
        IsLocked = true;
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
    }

    /// <summary>
    /// Returns an unlocked copy for editing. Original template remains locked.
    /// Call Lock() on the new template before attaching it to a cycle.
    /// </summary>
    public ReviewTemplate CloneForEdit(string newName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(newName);
        var clone = new ReviewTemplate(Guid.NewGuid(), TenantId, newName.Trim(),
            ApplicableCycleType, IncludesSelfReview, IncludesManagerReview, IncludesPeerReview);
        foreach (var s in _sections)
            clone._sections.Add(s);
        return clone;
    }

    private void EnsureNotLocked()
    {
        if (IsLocked)
            throw new InvalidOperationException("Cannot modify a locked ReviewTemplate.");
    }

    private void ValidateSectionWeights()
    {
        if (_sections.Count == 0) return;
        var total = _sections.Sum(s => s.Weight);
        if (Math.Abs(total - 1.0m) > 0.001m)
            throw new InvalidOperationException(
                $"Section weights must sum to 1.0. Current sum: {total:F3}.");
    }
}
