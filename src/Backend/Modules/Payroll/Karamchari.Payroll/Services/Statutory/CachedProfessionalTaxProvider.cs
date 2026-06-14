// -----------------------------------------------------------------------
// <copyright file="CachedProfessionalTaxProvider.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

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
            return new ProfessionalTaxResult(0m, false, $"No matching PT slab found for gross â‚¹{monthlyGross:N2} in state {stateCode}.");
        }

        return new ProfessionalTaxResult(match.MonthlyTaxAmount, true, $"PT Slab: â‚¹{match.MinGross:N0}-â‚¹{match.MaxGross:N0} (â‚¹{match.MonthlyTaxAmount})");
    }

    /// <inheritdoc/>
    public async Task PrimeAsync(FinancialYear year, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(year);

        var cacheKey = CacheKey(year);
        if (_cache.TryGetValue(cacheKey, out _))
        {
            return;
        }

        var slabs = await _repository.GetSlabsAsync(year);
        _cache.Set(cacheKey, BuildSlabMap(slabs), CacheDuration);
    }

    private Dictionary<string, List<ProfessionalTaxSlab>> GetCachedSlabs(FinancialYear year)
    {
        return _cache.GetOrCreate(CacheKey(year), entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;
            // Fallback path only: the batch pipeline pre-warms this cache via PrimeAsync
            // from an async context. This synchronous block is reached only on a cache
            // miss outside that path (e.g. an ad-hoc single payslip), so it costs at most
            // one blocking DB round-trip per year-key per hour rather than one per employee.
            var slabs = _repository.GetSlabsAsync(year).GetAwaiter().GetResult();
            return BuildSlabMap(slabs);
        }) ?? new Dictionary<string, List<ProfessionalTaxSlab>>();
    }

    private static string CacheKey(FinancialYear year) => $"PT_Slabs_{year.StartYear}_{year.EndYear}";

    private static Dictionary<string, List<ProfessionalTaxSlab>> BuildSlabMap(IEnumerable<ProfessionalTaxSlab> slabs) =>
        slabs
            .Where(s => s.IsActive)
            .GroupBy(s => s.StateCode)
            .ToDictionary(g => g.Key, g => g.ToList());
}
