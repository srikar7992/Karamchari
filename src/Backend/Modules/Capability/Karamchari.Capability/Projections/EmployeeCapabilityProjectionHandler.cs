using System.Threading;
using System.Threading.Tasks;
using Karamchari.Capability.Domain.Learning.Events;
using Karamchari.Capability.Domain.Marketplace;
using Karamchari.Capability.Persistence;
using Karamchari.Core.Projections;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Capability.Projections;

/// <summary>
/// Handles updates to the employee capability projection when learning enrollment completes.
/// </summary>
public sealed class EmployeeCapabilityProjectionHandler : IProjectionHandler<LearningEnrollmentCompleted>
{
    private readonly CapabilityDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="EmployeeCapabilityProjectionHandler"/> class.
    /// </summary>
    public EmployeeCapabilityProjectionHandler(CapabilityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public async Task HandleAsync(LearningEnrollmentCompleted domainEvent, CancellationToken cancellationToken)
    {
        var projection = await _dbContext.Set<EmployeeCapabilityProjection>()
            .FirstOrDefaultAsync(p => p.TenantId == domainEvent.TenantId && p.EmployeeId == domainEvent.EmployeeId, cancellationToken);

        if (projection is null)
        {
            projection = new EmployeeCapabilityProjection
            {
                EmployeeId = domainEvent.EmployeeId,
                TenantId = domainEvent.TenantId,
                EmployeeName = $"Employee {domainEvent.EmployeeId}",
                VerifiedSkills = [],
                ActiveGigsCount = 0,
                LastUpdatedAtUtc = domainEvent.CompletedAtUtc
            };
            _dbContext.Set<EmployeeCapabilityProjection>().Add(projection);
        }

        // Fetch skills granted by module
        var module = await _dbContext.LearningModules
            .Include(m => m.SkillTargets)
            .FirstOrDefaultAsync(m => m.Id == domainEvent.ModuleId, cancellationToken);

        if (module is not null)
        {
            foreach (var target in module.SkillTargets)
            {
                var skillName = $"Skill-{target.SkillId}";
                if (!projection.VerifiedSkills.Contains(skillName))
                {
                    projection.VerifiedSkills.Add(skillName);
                }
            }
        }

        projection.LastUpdatedAtUtc = domainEvent.CompletedAtUtc;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
