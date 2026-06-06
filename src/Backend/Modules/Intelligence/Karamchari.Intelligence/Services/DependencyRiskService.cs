using Karamchari.Intelligence.Domain.Workforce;
using Karamchari.Intelligence.Persistence;
using Karamchari.Intelligence.Services.Scoring;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Karamchari.Intelligence.Services;

/// <summary>
/// Nightly upsert of <see cref="EmployeeDependencyScore"/> for all employees in a tenant.
/// Reads dependency-related signal records and runs <see cref="DependencyRiskCalculator"/>.
/// Called by <see cref="WorkforceIntelligenceRecomputeJob"/> once per tenant.
/// </summary>
public sealed class DependencyRiskService
{
    private readonly IntelligenceDbContext _db;
    private readonly ILogger<DependencyRiskService> _logger;

    public DependencyRiskService(IntelligenceDbContext db, ILogger<DependencyRiskService> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Recalculates dependency risk scores for every employee in <paramref name="tenantId"/>
    /// that has at least one dependency-related signal.
    /// </summary>
    public async Task RecalculateTenantDependencyAsync(string tenantId, CancellationToken ct = default)
    {
        var dependencySignalTypes = new[]
        {
            WorkforceSignalType.CriticalShiftCoverageRatio,
            WorkforceSignalType.CrossTrainingDepth,
            WorkforceSignalType.UniqueRoleFlag,
            WorkforceSignalType.ApprovalAuthorityIndex
        };

        var employeeIds = await _db.WorkforceSignalRecords
            .Where(s => s.TenantId == tenantId && dependencySignalTypes.Contains(s.SignalType))
            .Select(s => s.EmployeeId)
            .Distinct()
            .ToListAsync(ct);

        if (employeeIds.Count == 0) return;

        _logger.LogInformation(
            "Recalculating dependency risk for {Count} employees in tenant {TenantId}",
            employeeIds.Count, tenantId);

        foreach (var employeeId in employeeIds)
        {
            try
            {
                await UpsertDependencyScoreAsync(tenantId, employeeId, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Dependency risk recalculation failed for employee {EmployeeId}", employeeId);
            }
        }
    }

    private async Task UpsertDependencyScoreAsync(
        string tenantId, Guid employeeId, CancellationToken ct)
    {
        decimal Latest(WorkforceSignalType t)
        {
            return _db.WorkforceSignalRecords
                .Where(s => s.TenantId == tenantId && s.EmployeeId == employeeId && s.SignalType == t)
                .OrderByDescending(s => s.SignalDate)
                .Select(s => s.Value)
                .FirstOrDefault();
        }

        var input = new DependencyRiskInput(
            CriticalShiftOwnershipRatio: Latest(WorkforceSignalType.CriticalShiftCoverageRatio),
            CrossTrainingDepth: (int)Latest(WorkforceSignalType.CrossTrainingDepth),
            UniqueRoleFlag: (int)Latest(WorkforceSignalType.UniqueRoleFlag),
            ApprovalAuthorityIndex: (int)Latest(WorkforceSignalType.ApprovalAuthorityIndex));

        var result = DependencyRiskCalculator.Calculate(input);

        var existing = await _db.EmployeeDependencyScores
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.EmployeeId == employeeId, ct);

        if (existing == null)
        {
            _db.EmployeeDependencyScores.Add(EmployeeDependencyScore.Create(
                tenantId, employeeId, result.Score, result.RiskLevel,
                result.CriticalShiftComponent, result.CrossTrainingComponent,
                result.UniqueRoleComponent, result.ApprovalComponent));
        }
        else
        {
            existing.Refresh(result.Score, result.RiskLevel,
                result.CriticalShiftComponent, result.CrossTrainingComponent,
                result.UniqueRoleComponent, result.ApprovalComponent);
        }

        await _db.SaveChangesAsync(ct);
    }
}
