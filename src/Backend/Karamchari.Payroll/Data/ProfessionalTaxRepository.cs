namespace Karamchari.Payroll.Data;

using Karamchari.Payroll.Domain.Statutory;
using Karamchari.Payroll.Services.Statutory;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// EF Core implementation of the PT repository.
/// </summary>
public sealed class ProfessionalTaxRepository : IProfessionalTaxRepository
{
    private readonly PayrollDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProfessionalTaxRepository"/> class.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    public ProfessionalTaxRepository(PayrollDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    /// <inheritdoc/>
    public async Task<List<ProfessionalTaxSlab>> GetSlabsAsync(FinancialYear year)
    {
        return await _dbContext.Set<ProfessionalTaxSlab>()
            .Where(s => s.FinancialYearStart == year.StartYear && s.FinancialYearEnd == year.EndYear)
            .Where(s => s.IsActive)
            .ToListAsync();
    }
}
