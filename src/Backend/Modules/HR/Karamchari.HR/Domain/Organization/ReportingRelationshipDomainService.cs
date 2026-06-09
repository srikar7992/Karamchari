namespace Karamchari.HR.Domain.Organization;

using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Domain service providing business logic and validation for reporting relationships.
/// </summary>
public sealed class ReportingRelationshipDomainService
{
    /// <summary>
    /// Validates that a proposed reporting relationship date range does not overlap with any existing reporting relationships for the same direct report position.
    /// </summary>
    /// <param name="directReportPositionId">The position ID of the direct report.</param>
    /// <param name="from">The starting date and time of the proposed relationship.</param>
    /// <param name="to">The ending date and time of the proposed relationship, if defined.</param>
    /// <param name="existingRelationships">The list of all existing relationships for comparison.</param>
    /// <param name="excludeRelationshipId">An optional relationship ID to exclude from the overlap check (e.g. when updating an existing relationship).</param>
    /// <returns>True if no overlap is detected; otherwise, false.</returns>
    public static bool ValidateNoOverlap(
        Guid directReportPositionId,
        DateTimeOffset from,
        DateTimeOffset? to,
        IEnumerable<ReportingRelationship> existingRelationships,
        Guid? excludeRelationshipId = null)
    {
        var otherRelationships = existingRelationships
            .Where(r => r.DirectReportPositionId == directReportPositionId);

        if (excludeRelationshipId.HasValue)
        {
            otherRelationships = otherRelationships.Where(r => r.Id != excludeRelationshipId.Value);
        }

        foreach (var rel in otherRelationships)
        {
            var rTo = rel.EffectiveTo ?? DateTimeOffset.MaxValue;
            var currentTo = to ?? DateTimeOffset.MaxValue;

            if (from < rTo && currentTo > rel.EffectiveFrom)
            {
                return false; // Overlap detected
            }
        }

        return true;
    }
}
