using Karamchari.Payroll.Data;
using Karamchari.Payroll.Domain;
using Karamchari.Payroll.Domain.Compliance;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Payroll.Services.Compliance;

/// <summary>
/// Service for managing statutory compliance orchestration, including file generation, 
/// snapshotting, and filing status tracking.
/// </summary>
public interface IComplianceService
{
    /// <summary>Generates the EPF ECR file for a specific payroll run.</summary>
    Task<(string fileName, byte[] content)> GetEpfEcrAsync(Guid runId);
    
    /// <summary>Generates the ESIC Monthly Return file for a specific payroll run.</summary>
    Task<(string fileName, byte[] content)> GetEsicMonthlyAsync(Guid runId);
    
    /// <summary>Generates the TDS Form 24Q file for a specific payroll run.</summary>
    Task<(string fileName, byte[] content)> GetTdsForm24QAsync(Guid runId);
    
    /// <summary>Marks a compliance return as filed with a government reference number.</summary>
    Task MarkAsFiledAsync(Guid runId, ComplianceType type, string referenceNumber, string filedBy);
    
    /// <summary>Retrieves a summary of compliance health and risk for the dashboard.</summary>
    Task<object> GetComplianceHealthAsync();
}

/// <summary>
/// Implementation of <see cref="IComplianceService"/>.
/// </summary>
public sealed class ComplianceService : IComplianceService
{
    private readonly PayrollDbContext _dbContext;
    private readonly IEcrGenerator _ecrGenerator;
    private readonly IEsicGenerator _esicGenerator;
    private readonly ITdsGenerator _tdsGenerator;
    private readonly IComplianceRiskEngine _riskEngine;

    /// <summary>
    /// Initializes a new instance of the <see cref="ComplianceService"/> class.
    /// </summary>
    public ComplianceService(
        PayrollDbContext dbContext,
        IEcrGenerator ecrGenerator,
        IEsicGenerator esicGenerator,
        ITdsGenerator tdsGenerator,
        IComplianceRiskEngine riskEngine)
    {
        _dbContext = dbContext;
        _ecrGenerator = ecrGenerator;
        _esicGenerator = esicGenerator;
        _tdsGenerator = tdsGenerator;
        _riskEngine = riskEngine;
    }

    /// <inheritdoc/>
    public async Task<(string fileName, byte[] content)> GetEpfEcrAsync(Guid runId)
    {
        var snapshot = await _dbContext.ComplianceSnapshots
            .FirstOrDefaultAsync(s => s.PayrollRunId == runId && s.Type == ComplianceType.EpfEcr);

        if (snapshot != null) return (snapshot.FileName, snapshot.Content);

        var (entries, profiles) = await FetchDataAsync(runId);
        
        await EnsureFilingExistsAsync(runId, ComplianceType.EpfEcr, entries.First().Month, entries.First().Year);

        var records = entries.Select(e =>
        {
            var p = profiles[e.EmployeeId];
            var basic = e.Earnings.GetValueOrDefault("Basic", 0);
            var epfWage = p.OptedForVoluntaryPF ? basic : Math.Min(basic, 15000);
            
            var epfTotal = e.Deductions.GetValueOrDefault("EPF", 0);
            var epsContribution = Math.Round(Math.Min(epfWage * 0.0833m, 1250), 0, MidpointRounding.AwayFromZero);
            
            return new EcrRecord
            {
                Uan = p.Uan,
                MemberName = p.EmployeeName,
                GrossWages = e.MonthlyGross,
                EpfWages = epfWage,
                EpsWages = Math.Min(epfWage, 15000),
                EdliWages = Math.Min(epfWage, 15000),
                EpfContribution = epfTotal,
                EpsContribution = epsContribution,
                EpfEpsDiff = epfTotal - epsContribution,
                NcpDays = 0, // Placeholder
                RefundOfAdvances = 0
            };
        }).ToList();

        var content = System.Text.Encoding.ASCII.GetBytes(_ecrGenerator.Generate(records));
        var fileName = $"EPF_ECR_{runId}.txt";

        await SaveSnapshotAsync(runId, ComplianceType.EpfEcr, fileName, content);
        
        return (fileName, content);
    }

    /// <inheritdoc/>
    public async Task<(string fileName, byte[] content)> GetEsicMonthlyAsync(Guid runId)
    {
        var snapshot = await _dbContext.ComplianceSnapshots
            .FirstOrDefaultAsync(s => s.PayrollRunId == runId && s.Type == ComplianceType.EsicMonthly);

        if (snapshot != null) return (snapshot.FileName, snapshot.Content);

        var (entries, profiles) = await FetchDataAsync(runId);

        await EnsureFilingExistsAsync(runId, ComplianceType.EsicMonthly, entries.First().Month, entries.First().Year);

        var records = entries
            .Where(e => e.MonthlyGross <= 21000)
            .Select(e =>
            {
                var p = profiles[e.EmployeeId];
                return new EsicRecord
                {
                    InsuranceNumber = p.EsicNumber,
                    EmployeeName = p.EmployeeName,
                    GrossWages = e.MonthlyGross,
                    EmployeeContribution = e.Deductions.GetValueOrDefault("ESIC", 0),
                    EmployerContribution = Math.Round(e.MonthlyGross * 0.0325m, 0, MidpointRounding.AwayFromZero),
                    DaysWorked = 30 // Placeholder
                };
            }).ToList();

        var content = _esicGenerator.Generate(records);
        var fileName = $"ESIC_Monthly_{runId}.xlsx";

        await SaveSnapshotAsync(runId, ComplianceType.EsicMonthly, fileName, content);

        return (fileName, content);
    }

    /// <inheritdoc/>
    public async Task<(string fileName, byte[] content)> GetTdsForm24QAsync(Guid runId)
    {
        var snapshot = await _dbContext.ComplianceSnapshots
            .FirstOrDefaultAsync(s => s.PayrollRunId == runId && s.Type == ComplianceType.TdsForm24Q);

        if (snapshot != null) return (snapshot.FileName, snapshot.Content);

        var (entries, profiles) = await FetchDataAsync(runId);

        await EnsureFilingExistsAsync(runId, ComplianceType.TdsForm24Q, entries.First().Month, entries.First().Year);

        var records = entries.Select(e =>
        {
            var p = profiles[e.EmployeeId];
            return new TdsRecord
            {
                Pan = p.Pan,
                EmployeeName = p.EmployeeName,
                AnnualIncome = p.AnnualCTC,
                TotalTax = e.TdsDeducted * 12, // Simplified projection
                TdsDeducted = e.TdsDeducted,
                RemainingTax = (e.TdsDeducted * 12) - e.TdsDeducted
            };
        }).ToList();

        var content = System.Text.Encoding.UTF8.GetBytes(_tdsGenerator.Generate(records));
        var fileName = $"TDS_Form24Q_{runId}.txt";

        await SaveSnapshotAsync(runId, ComplianceType.TdsForm24Q, fileName, content);

        return (fileName, content);
    }

    /// <inheritdoc/>
    public async Task MarkAsFiledAsync(Guid runId, ComplianceType type, string referenceNumber, string filedBy)
    {
        var filing = await _dbContext.ComplianceFilings
            .FirstOrDefaultAsync(f => f.PayrollRunId == runId && f.Type == type);

        if (filing == null)
        {
            // Auto-create if missing (e.g., if filing is done without explicit download through the UI)
            filing = ComplianceFiling.CreatePending(string.Empty, runId, type, DateTime.UtcNow.AddDays(15));
            _dbContext.ComplianceFilings.Add(filing);
        }

        filing.MarkAsFiled(referenceNumber, filedBy);
        await _dbContext.SaveChangesAsync();
    }

    /// <inheritdoc/>
    public async Task<object> GetComplianceHealthAsync()
    {
        var filings = await _dbContext.ComplianceFilings
            .OrderByDescending(f => f.DueDate)
            .Take(20)
            .ToListAsync();

        var summaries = filings.Select(f => {
            // Calculate actual liability from the ledger for this filing
            var ledgerEntries = _dbContext.PayrollLedger.Where(e => e.RunId == f.PayrollRunId).ToList();
            
            decimal amount = f.Type switch {
                ComplianceType.EpfEcr => ledgerEntries.Sum(e => e.Deductions.GetValueOrDefault("EPF", 0) + e.Earnings.GetValueOrDefault("EmployerPF", 0)),
                ComplianceType.EsicMonthly => ledgerEntries.Sum(e => e.Deductions.GetValueOrDefault("ESIC", 0) + e.Earnings.GetValueOrDefault("EmployerESIC", 0)),
                ComplianceType.TdsForm24Q => ledgerEntries.Sum(e => e.TdsDeducted),
                _ => 0
            };
            
            var risk = _riskEngine.Evaluate(f, amount);
            
            return new {
                f.Id,
                f.PayrollRunId,
                f.Type,
                f.Status,
                f.DueDate,
                f.FiledAtUtc,
                Risk = risk
            };
        });

        return new {
            TotalPending = filings.Count(f => f.Status != FilingStatus.Filed),
            HighRiskCount = summaries.Count(s => s.Risk.RiskLevel == "High" || s.Risk.RiskLevel == "Critical"),
            Filings = summaries
        };
    }

    private async Task<(List<PayrollLedgerEntry> entries, Dictionary<Guid, PayrollProfile> profiles)> FetchDataAsync(Guid runId)
    {
        var entries = await _dbContext.PayrollLedger.Where(e => e.RunId == runId).ToListAsync();
        var employeeIds = entries.Select(e => e.EmployeeId).ToList();
        var profiles = await _dbContext.PayrollProfiles
            .Where(p => employeeIds.Contains(p.EmployeeId))
            .ToDictionaryAsync(p => p.EmployeeId);
            
        return (entries, profiles);
    }

    private async Task SaveSnapshotAsync(Guid runId, ComplianceType type, string fileName, byte[] content)
    {
        var snapshot = ComplianceSnapshot.Create(string.Empty, runId, type, fileName, content, "System");
        _dbContext.ComplianceSnapshots.Add(snapshot);
        await _dbContext.SaveChangesAsync();
    }

    private async Task EnsureFilingExistsAsync(Guid runId, ComplianceType type, int month, int year)
    {
        var existing = await _dbContext.ComplianceFilings
            .AnyAsync(f => f.PayrollRunId == runId && f.Type == type);

        if (existing) return;

        // Deadline is 15th of the following month for EPF/ESIC
        var nextMonth = month == 12 ? 1 : month + 1;
        var nextYear = month == 12 ? year + 1 : year;
        var dueDate = new DateTime(nextYear, nextMonth, 15, 0, 0, 0, DateTimeKind.Utc);

        var filing = ComplianceFiling.CreatePending(string.Empty, runId, type, dueDate);
        _dbContext.ComplianceFilings.Add(filing);
        await _dbContext.SaveChangesAsync();
    }
}
