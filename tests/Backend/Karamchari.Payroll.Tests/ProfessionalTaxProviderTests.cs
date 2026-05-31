namespace Karamchari.Payroll.Tests;

using Karamchari.Payroll.Domain.Statutory;
using Karamchari.Payroll.Services.Statutory;
using Microsoft.Extensions.Caching.Memory;
using NSubstitute;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public class ProfessionalTaxProviderTests
{
    private static readonly FinancialYear FY2026 = new(2026, 2027);

    [Fact]
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void ProviderShouldMatchSlabBasedOnGross()
    {
        // Arrange
        var slabs = new List<ProfessionalTaxSlab>
        {
            ProfessionalTaxSlab.Create("TS", 0, 15000, 0, 2026, 2027),
            ProfessionalTaxSlab.Create("TS", 15001, 20000, 150, 2026, 2027),
            ProfessionalTaxSlab.Create("TS", 20001, 999999, 200, 2026, 2027)
        };

        var repo = Substitute.For<IProfessionalTaxRepository>();
        repo.GetSlabsAsync(FY2026).Returns(Task.FromResult(slabs));

        var cache = new MemoryCache(new MemoryCacheOptions());
        var provider = new CachedProfessionalTaxProvider(repo, cache);

        // Act
        var result = provider.GetTaxAmount("TS", 25000, 4, FY2026);

        // Assert
        Assert.True(result.IsApplicable);
        Assert.Equal(200, result.Amount);
    }

    [Fact]
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void ProviderShouldHandleMonthOverride()
    {
        // Arrange
        var slabs = new List<ProfessionalTaxSlab>
        {
            ProfessionalTaxSlab.Create("MH", 0, 10000, 200, 2026, 2027), // Default
            ProfessionalTaxSlab.Create("MH", 0, 10000, 300, 2026, 2027, applicableMonth: 2, priority: 10) // Feb override
        };

        var repo = Substitute.For<IProfessionalTaxRepository>();
        repo.GetSlabsAsync(FY2026).Returns(Task.FromResult(slabs));

        var cache = new MemoryCache(new MemoryCacheOptions());
        var provider = new CachedProfessionalTaxProvider(repo, cache);

        // Act
        var resultFeb = provider.GetTaxAmount("MH", 5000, 2, FY2026);
        var resultApril = provider.GetTaxAmount("MH", 5000, 4, FY2026);

        // Assert
        Assert.Equal(300, resultFeb.Amount);
        Assert.Equal(200, resultApril.Amount);
    }

    [Fact]
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void ProviderShouldCacheSlabsAndNotHitRepoTwice()
    {
        // Arrange
        var repo = Substitute.For<IProfessionalTaxRepository>();
        repo.GetSlabsAsync(Arg.Any<FinancialYear>()).Returns(Task.FromResult(new List<ProfessionalTaxSlab>()));

        var cache = new MemoryCache(new MemoryCacheOptions());
        var provider = new CachedProfessionalTaxProvider(repo, cache);

        // Act
        provider.GetTaxAmount("TS", 10000, 4, FY2026);
        provider.GetTaxAmount("TS", 15000, 5, FY2026);

        // Assert
        repo.Received(1).GetSlabsAsync(Arg.Any<FinancialYear>());
    }
}
