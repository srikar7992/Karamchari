using Karamchari.Benefits.Domain;
using Karamchari.Benefits.Domain.Deductions;
using Karamchari.Benefits.Domain.Events;
using Karamchari.Benefits.Persistence;
using Karamchari.Payroll.Contracts;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Karamchari.Benefits.Consumers;

public sealed class PayrollRunInitiatedBenefitsConsumer(
    BenefitsDbContext db,
    ILogger<PayrollRunInitiatedBenefitsConsumer> logger) : IConsumer<StartPayrollRunCommand>
{
    public async Task Consume(ConsumeContext<StartPayrollRunCommand> context)
    {
        var msg = context.Message;

        var activeEnrollments = await db.Enrollments
            .Where(e => e.TenantId == msg.TenantId && e.Status != EnrollmentStatus.WaivedAllCoverage)
            .ToListAsync(context.CancellationToken);

        if (activeEnrollments.Count == 0) return;

        var enrollmentIds = activeEnrollments.Select(e => e.Id).ToList();
        var activeElections = await db.Elections
            .Where(el => enrollmentIds.Contains(el.EnrollmentId) && el.IsActive)
            .ToListAsync(context.CancellationToken);

        if (activeElections.Count == 0) return;

        var enrollmentMap = activeEnrollments.ToDictionary(e => e.Id);
        var planIds = activeElections.Select(el => el.PlanId).Distinct().ToList();
        var plans = await db.Plans
            .Where(p => planIds.Contains(p.Id))
            .ToListAsync(context.CancellationToken);
        var planMap = plans.ToDictionary(p => p.Id);

        var records = new List<BenefitDeductionRecord>();
        foreach (var election in activeElections)
        {
            if (!planMap.TryGetValue(election.PlanId, out var plan)) continue;
            if (!enrollmentMap.TryGetValue(election.EnrollmentId, out var enrollment)) continue;

            var tier = plan.Tiers.Count > 0 ? plan.Tiers[0] : null;
            if (tier is null) continue;

            var exists = await db.DeductionRecords.AnyAsync(
                r => r.TenantId == msg.TenantId && r.EmployeeId == enrollment.EmployeeId
                     && r.PlanId == election.PlanId.Value && r.PayPeriod == msg.PeriodName,
                context.CancellationToken);
            if (exists) continue;

            records.Add(new BenefitDeductionRecord
            {
                TenantId = msg.TenantId,
                EmployeeId = enrollment.EmployeeId,
                PlanId = election.PlanId.Value,
                PayPeriod = msg.PeriodName,
                EmployeeDeduction = tier.EmployeeMonthlyPremium,
                EmployerContribution = tier.EmployerMonthlyPremium
            });
        }

        db.DeductionRecords.AddRange(records);
        await db.SaveChangesAsync(context.CancellationToken);

        foreach (var r in records)
            await context.Publish(new BenefitDeductionCalculated(
                r.Id, r.TenantId, r.EmployeeId, r.PayPeriod, r.EmployeeDeduction, r.EmployerContribution));

        logger.LogInformation("Benefits: {Count} deduction records created for period {Period}", records.Count, msg.PeriodName);
    }
}
