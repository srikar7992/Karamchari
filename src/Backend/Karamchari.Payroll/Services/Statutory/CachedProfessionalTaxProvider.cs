namespace Karamchari.Payroll.Services.Statutory;

using Karamchari.Payroll.Domain.Statutory;
using Microsoft.Extensions.Caching.Memory;

/// <summary>
/// A high-performance, cached implementation of the Professional Tax provider.
/// </summary>
public sealed class CachedProfessionalTaxProvider : IProfessionalTaxProvider
{
    private readonly IProfessionalTaxRepository _repository;
    private readonly IMemoryCache _cache;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(1);

    /// <summary>
    /// Initializes a new instance of the <see cref="CachedProfessionalTaxProvider"/> class.
    /// </summary>
    public CachedProfessionalTaxProvider(IProfessionalTaxRepository repository, IMemoryCache cache)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    /// <inheritdoc/>
    public ProfessionalTaxResult GetTaxAmount(string stateCode, decimal monthlyGross, int month, FinancialYear year)
    {
        ArgumentNullException.ThrowIfNull(year);
        var slabs = GetCachedSlabs(year);
        
        if (!slabs.TryGetValue(stateCode, out var stateSlabs))
        {
            return new ProfessionalTaxResult(0m, false, $"No PT slabs configured for state {stateCode}.");
        }

        var match = stateSlabs
            .Where(s => !s.ApplicableMonth.HasValue || s.ApplicableMonth.Value == month)
            .Where(s => monthlyGross >= s.MinGross && monthlyGross <= s.MaxGross)
            .OrderByDescending(s => s.Priority)
            .FirstOrDefault();

        if (match == null)
        {
            return new ProfessionalTaxResult(0m, false, $"No matching PT slab found for gross ₹{monthlyGross:N2} in state {stateCode}.");
        }

        return new ProfessionalTaxResult(match.MonthlyTaxAmount, true, $"PT Slab: ₹{match.MinGross:N0}-₹{match.MaxGross:N0} (₹{match.MonthlyTaxAmount})");
    }

    private Dictionary<string, List<ProfessionalTaxSlab>> GetCachedSlabs(FinancialYear year)
    {
        string cacheKey = $"PT_Slabs_{year.StartYear}_{year.EndYear}";
        
        return _cache.GetOrCreate(cacheKey, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;
            // Note: In a real app, this would be a synchronous block or a pre-warmed cache
            // since we are calling it from a synchronous StatutoryPipeline.
            // For now, we'll use Task.Run(...).Result as a temporary bridge or assume it's pre-loaded.
            var slabs = _repository.GetSlabsAsync(year).GetAwaiter().GetResult();
            
            return slabs
                .Where(s => s.IsActive)
                .GroupBy(s => s.StateCode)
                .ToDictionary(g => g.Key, g => g.ToList());
        }) ?? new Dictionary<string, List<ProfessionalTaxSlab>>();
    }
}
