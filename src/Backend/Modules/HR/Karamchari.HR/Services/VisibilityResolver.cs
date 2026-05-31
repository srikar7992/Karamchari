using Karamchari.HR.Domain.Relationships;
using Karamchari.HR.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.HR.Services;

internal sealed class VisibilityResolver : IVisibilityResolver
{
    private readonly HRDbContext _db;

    public VisibilityResolver(HRDbContext db) => _db = db;

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public async Task<IReadOnlySet<Guid>> ResolveVisibleEmployeeIdsAsync(
        Guid actorId, VisibilityScope scope, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var types = GetRelationshipTypes(scope);

        var ids = await _db.EmployeeRelationships
            .AsNoTracking()
            .Where(r => r.FromEmployeeId == actorId
                     && types.Contains(r.RelationshipType)
                     && (r.EffectiveTo == null || r.EffectiveTo >= today))
            .Select(r => r.ToEmployeeId)
            .ToHashSetAsync(ct);

        return ids;
    }

    private static RelationshipType[] GetRelationshipTypes(VisibilityScope scope) => scope switch
    {
        VisibilityScope.DirectOnly =>
            [RelationshipType.DirectManager, RelationshipType.ActingManager],
        VisibilityScope.DirectAndFunctional =>
            [RelationshipType.DirectManager, RelationshipType.ActingManager,
             RelationshipType.DottedLineManager, RelationshipType.ProjectManager],
        VisibilityScope.SkipLevel =>
            [RelationshipType.DirectManager, RelationshipType.ActingManager,
             RelationshipType.DottedLineManager, RelationshipType.SkipLevelManager],
        VisibilityScope.CalibrationPanel =>
            [RelationshipType.CalibrationCommitteeMember],
        VisibilityScope.AllManaged =>
            [RelationshipType.DirectManager, RelationshipType.ActingManager,
             RelationshipType.DottedLineManager, RelationshipType.ProjectManager,
             RelationshipType.SkipLevelManager],
        _ => throw new ArgumentOutOfRangeException(nameof(scope)),
    };
}
