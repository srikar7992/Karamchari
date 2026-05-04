namespace Karamchari.Payroll.Data;

using Karamchari.Payroll.Domain.Statutory;
using Karamchari.Payroll.Services.Statutory;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// EF Core implementation of the IT declaration repository.
/// </summary>
public sealed class ITDeclarationRepository : IITDeclarationRepository
{
    private readonly PayrollDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="ITDeclarationRepository"/> class.
    /// </summary>
    public ITDeclarationRepository(PayrollDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ITDeclaration>> GetApprovedDeclarationsAsync(Guid employeeId, int financialYear)
    {
        return await _dbContext.ITDeclarations
            .Where(x => 
                x.EmployeeId == employeeId && 
                x.FinancialYear == financialYear && 
                x.Status == VerificationStatus.Approved)
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<ITDeclaration?> GetLatestAsync(Guid employeeId, int financialYear, string category)
    {
        return await _dbContext.ITDeclarations
            .Where(x => 
                x.EmployeeId == employeeId && 
                x.FinancialYear == financialYear && 
                x.Category == category)
            .OrderByDescending(x => x.Version)
            .FirstOrDefaultAsync();
    }

    /// <inheritdoc/>
    public async Task<List<ITDeclaration>> GetPendingReviewAsync()
    {
        return await _dbContext.ITDeclarations
            .Where(x => x.Status == VerificationStatus.PendingReview || x.Status == VerificationStatus.Submitted)
            .OrderBy(x => x.SubmittedAt)
            .Take(100)
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<ITDeclaration?> GetByIdAsync(Guid id)
    {
        return await _dbContext.ITDeclarations
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    /// <inheritdoc/>
    public async Task UpsertAsync(ITDeclaration declaration)
    {
        var existing = await _dbContext.ITDeclarations.FindAsync(declaration.Id);
        if (existing == null)
        {
            _dbContext.ITDeclarations.Add(declaration);
        }
        else
        {
            _dbContext.Entry(existing).CurrentValues.SetValues(declaration);
        }
    }

    /// <inheritdoc/>
    public async Task SaveChangesAsync() => await _dbContext.SaveChangesAsync();
}
