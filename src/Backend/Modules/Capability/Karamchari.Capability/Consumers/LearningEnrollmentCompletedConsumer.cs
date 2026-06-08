using Karamchari.Capability.Domain.Learning.Events;
using Karamchari.Capability.Domain.Primitives;
using Karamchari.Capability.Domain.Skills;
using Karamchari.Capability.Persistence;
using Karamchari.Core.Messaging;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Capability.Consumers;

/// <summary>
/// When a LearningEnrollment completes, add or upgrade the corresponding VerifiedSkill
/// on the employee's CapabilityProfile for each ModuleSkillTarget the module defines.
/// </summary>
public sealed class LearningEnrollmentCompletedConsumer(CapabilityDbContext db)
    : IConsumer<EnterpriseEventEnvelope<LearningEnrollmentCompleted>>
{
    public async Task Consume(ConsumeContext<EnterpriseEventEnvelope<LearningEnrollmentCompleted>> context)
    {
        var envelope = context.Message;
        var payload = envelope.Payload;
        var tenantId = envelope.TenantId;

        var module = await db.LearningModules
            .Include(m => m.SkillTargets)
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == payload.ModuleId, context.CancellationToken);

        if (module is null || module.SkillTargets.Count == 0)
            return;

        var profile = await db.CapabilityProfiles
            .Include(p => p.Skills)
            .FirstOrDefaultAsync(
                p => p.TenantId == tenantId && p.EmployeeId == payload.EmployeeId,
                context.CancellationToken);

        if (profile is null)
        {
            profile = CapabilityProfile.Initialize(tenantId, payload.EmployeeId);
            db.CapabilityProfiles.Add(profile);
        }

        const string verifiedBy = "system:learning-completion";

        foreach (var target in module.SkillTargets)
        {
            var existing = profile.Skills.FirstOrDefault(s => s.SkillId == target.SkillId);

            if (existing is null)
            {
                profile.AddVerifiedSkill(
                    target.SkillId,
                    target.GrantedLevel,
                    $"learning:{payload.ModuleId}",
                    verifiedBy);
            }
            else if (existing.Level < target.GrantedLevel)
            {
                profile.UpdateSkillLevel(
                    target.SkillId,
                    target.GrantedLevel,
                    $"learning:{payload.ModuleId}",
                    verifiedBy);
            }
        }

        await db.SaveChangesAsync(context.CancellationToken);
    }
}
