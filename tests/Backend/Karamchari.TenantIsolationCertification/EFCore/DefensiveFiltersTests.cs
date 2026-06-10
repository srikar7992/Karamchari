// -----------------------------------------------------------------------
// <copyright file="DefensiveFiltersTests.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Data.Common;
using FluentAssertions;
using Karamchari.Core.Multitenancy;
using Karamchari.Core.Persistence.Filters;
using Karamchari.TenantIsolationCertification.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Karamchari.TenantIsolationCertification.EFCore;

public sealed class TenantQueryValidationInterceptorTests : IDisposable
{
    private readonly TenantTestContext _context;
    private readonly MockTenantProvider _tenantProvider;
    private readonly TenantQueryValidationInterceptor _interceptor;

    public TenantQueryValidationInterceptorTests()
    {
        _context = TenantTestContext.Create("acme");
        _tenantProvider = new MockTenantProvider("acme");
        var logger = Substitute.For<ILogger<TenantQueryValidationInterceptor>>();
        _interceptor = new TenantQueryValidationInterceptor(_tenantProvider, logger);
    }

    [Fact]
    public void Constructor_ShouldInitialize()
    {
        _interceptor.Should().NotBeNull();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}

public sealed class RawSqlTenantGuardTests : IDisposable
{
    private readonly TenantTestContext _context;
    private readonly MockTenantProvider _tenantProvider;
    private readonly RawSqlTenantGuard _guard;
    private readonly DbContext _dbContext;

    public RawSqlTenantGuardTests()
    {
        _context = TenantTestContext.Create("acme");
        _tenantProvider = new MockTenantProvider("acme");
        var logger = Substitute.For<ILogger<RawSqlTenantGuard>>();
        _guard = new RawSqlTenantGuard(_tenantProvider, logger);
        _dbContext = Substitute.For<DbContext>();
    }

    [Fact]
    public void GuardFromSqlRaw_WithValidSql_ShouldNotThrow()
    {
        var sql = "SELECT * FROM Employees WHERE TenantId = 'acme'";

        var action = () => _guard.GuardFromSqlRaw(sql, _dbContext);

        action.Should().NotThrow();
    }

    [Fact]
    public void GuardFromSqlRaw_WithoutTenantContext_ShouldThrow()
    {
        _tenantProvider.SetTenantContext(null);
        var sql = "SELECT * FROM Employees";

        var action = () => _guard.GuardFromSqlRaw(sql, _dbContext);

        action.Should().Throw<UnsafeQueryAccessException>()
            .WithMessage("*tenant context*");
    }

    [Fact]
    public void GuardFromSqlRaw_WithSqlInjectionPattern_ShouldThrow()
    {
        var sql = "SELECT * FROM Employees WHERE Id = 1; DROP TABLE Employees;--";

        var action = () => _guard.GuardFromSqlRaw(sql, _dbContext);

        action.Should().Throw<UnsafeQueryAccessException>()
            .WithMessage("*dangerous patterns*");
    }

    [Fact]
    public void GuardExecuteSqlRaw_WithValidSql_ShouldNotThrow()
    {
        var sql = "UPDATE Employees SET Name = 'Test' WHERE TenantId = 'acme'";

        var action = () => _guard.GuardExecuteSqlRaw(sql, _dbContext);

        action.Should().NotThrow();
    }

    [Fact]
    public void GuardExecuteSqlRaw_WithUnionSelect_ShouldThrow()
    {
        var sql = "SELECT * FROM Employees UNION SELECT * FROM Users";

        var action = () => _guard.GuardExecuteSqlRaw(sql, _dbContext);

        action.Should().Throw<UnsafeQueryAccessException>();
    }

    [Fact]
    public void SafeFromSql_WithParameters_ShouldValidateParameters()
    {
        var sql = "SELECT * FROM Employees WHERE Name = {0}";
        var parameters = new object[] { "Test" };

        var result = _guard.SafeFromSql(sql, parameters);

        result.Should().Be(sql);
    }

    [Fact]
    public void SafeFromSql_WithMaliciousParameter_ShouldThrow()
    {
        var sql = "SELECT * FROM Employees WHERE Name = {0}";
        var parameters = new object[] { "Test'; DROP TABLE Employees;--" };

        var action = () => _guard.SafeFromSql(sql, parameters);

        action.Should().Throw<UnsafeQueryAccessException>();
    }

    [Fact]
    public async Task GuardFromSqlRawAsync_WithValidSql_ShouldNotThrow()
    {
        var sql = "SELECT * FROM Employees WHERE TenantId = 'acme'";

        await _guard.GuardFromSqlRawAsync(sql, _dbContext);

        await Task.CompletedTask;
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}

public sealed class GlobalTenantQueryFilterTests : IDisposable
{
    private readonly TenantTestContext _context;

    public GlobalTenantQueryFilterTests()
    {
        _context = TenantTestContext.Create("acme");
    }

    [Fact]
    public void ApplyGlobalFilters_WithTenantOwnedEntity_ShouldApplyFilter()
    {
        var modelBuilder = new ModelBuilder();
        modelBuilder.Entity<TestTenantOwnedEntity>();

        GlobalTenantQueryFilter.ApplyGlobalFilters(modelBuilder, "acme");

        var entityType = modelBuilder.Model.FindEntityType(typeof(TestTenantOwnedEntity));
#pragma warning disable CS0618
        entityType!.GetQueryFilter().Should().NotBeNull();
#pragma warning restore CS0618
    }

    [Fact]
    public void IsEntityTenantFiltered_WithNonTenantEntity_ShouldReturnFalse()
    {
        var modelBuilder = new ModelBuilder();
        modelBuilder.Entity<TestNonTenantEntity>();

        // IMutableModel inherits from IReadOnlyModel, and so does IModel.
        // Finalize the model to get an IModel.
        var model = modelBuilder.FinalizeModel();

        var isFiltered = GlobalTenantQueryFilter.IsEntityTenantFiltered(
            typeof(TestNonTenantEntity), model);

        isFiltered.Should().BeFalse();
    }

    [Fact]
    public void GetTenantFilteredEntities_ShouldReturnOnlyTenantEntities()
    {
        var modelBuilder = new ModelBuilder();
        modelBuilder.Entity<TestTenantOwnedEntity>();
        modelBuilder.Entity<TestNonTenantEntity>();

        GlobalTenantQueryFilter.ApplyGlobalFilters(modelBuilder, "acme");

        var model = modelBuilder.FinalizeModel();
        var filteredEntities = GlobalTenantQueryFilter.GetTenantFilteredEntities(model).ToList();

        filteredEntities.Should().Contain(typeof(TestTenantOwnedEntity));
        filteredEntities.Should().NotContain(typeof(TestNonTenantEntity));
    }

    [Fact]
    public void RemoveGlobalFilters_ShouldClearAllFilters()
    {
        var modelBuilder = new ModelBuilder();
        modelBuilder.Entity<TestTenantOwnedEntity>();

        GlobalTenantQueryFilter.ApplyGlobalFilters(modelBuilder, "acme");
        GlobalTenantQueryFilter.RemoveGlobalFilters(modelBuilder);

        var entityType = modelBuilder.Model.FindEntityType(typeof(TestTenantOwnedEntity));
#pragma warning disable CS0618
        entityType!.GetQueryFilter().Should().BeNull();
#pragma warning restore CS0618
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}

public sealed class QueryFilterBypassDetectorTests : IDisposable
{
    private readonly TenantTestContext _context;
    private readonly QueryFilterBypassDetector _detector;
    private readonly MockTenantProvider _tenantProvider;

    public QueryFilterBypassDetectorTests()
    {
        _context = TenantTestContext.Create("acme");
        _tenantProvider = new MockTenantProvider("acme");
        var logger = Substitute.For<ILogger<QueryFilterBypassDetector>>();
        _detector = new QueryFilterBypassDetector(_tenantProvider, logger);
    }

    [Fact]
    public void DetectBypassAttempt_WithIgnoreQueryFilters_ShouldRaiseEvent()
    {
        bool eventRaised = false;
        _detector.BypassDetected += (s, e) => eventRaised = true;

        _detector.DetectBypassAttempt("IgnoreQueryFilters", "Test query");

        eventRaised.Should().BeTrue("Bypass detection should raise event");
    }

    [Fact]
    public void IsQueryBypass_WithIgnoreQueryFilters_ShouldReturnTrue()
    {
        var result = _detector.IsQueryBypass("IgnoreQueryFilters()");

        result.Should().BeTrue();
    }

    [Fact]
    public void IsQueryBypass_WithAsNoTracking_ShouldReturnTrue()
    {
        var result = _detector.IsQueryBypass("AsNoTracking()");

        result.Should().BeTrue();
    }

    [Fact]
    public void IsQueryBypass_WithNormalQuery_ShouldReturnFalse()
    {
        var result = _detector.IsQueryBypass("Where(e => e.Name == 'Test')");

        result.Should().BeFalse();
    }

    [Fact]
    public void IsSafeQuery_WithCorrectTenant_ShouldReturnTrue()
    {
        var result = QueryFilterBypassDetector.IsSafeQuery("SELECT * FROM Employees WHERE TenantId = 'acme'", "acme");

        result.Should().BeTrue();
    }

    [Fact]
    public void IsSafeQuery_WithMissingTenant_ShouldReturnFalse()
    {
        var result = QueryFilterBypassDetector.IsSafeQuery("SELECT * FROM Employees", "acme");

        result.Should().BeFalse();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}

public sealed class UnsafeQueryAccessExceptionTests
{
    [Fact]
    public void UnsafeQueryAccessException_WithDetails_ShouldPreserveProperties()
    {
        var exception = new UnsafeQueryAccessException(
            "Test message",
            "tenant-123",
            "SELECT * FROM Employees",
            isCrossTenantAttempt: true);

        exception.AttemptedTenantId.Should().Be("tenant-123");
        exception.QueryDetails.Should().Be("SELECT * FROM Employees");
        exception.IsCrossTenantAttempt.Should().BeTrue();
    }

    [Fact]
    public void ToString_ShouldIncludeAllDetails()
    {
        var exception = new UnsafeQueryAccessException(
            "Test message",
            "tenant-123",
            "SELECT * FROM Employees",
            isCrossTenantAttempt: true);

        var result = exception.ToString();

        result.Should().Contain("tenant-123");
        result.Should().Contain("SELECT * FROM Employees");
        result.Should().Contain("True");
    }
}

internal class TestTenantOwnedEntity : ITenantOwned
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string TenantId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

internal class TestNonTenantEntity
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
}

internal class MockTenantProvider : ITenantProvider
{
    private TenantExecutionEnvelope? _tenantContext;

    public MockTenantProvider(string tenantId)
    {
        _tenantContext = new TenantExecutionEnvelope(tenantId, "test-corr", "test-req", ExecutionSource.Test, TenantSource.JwtClaim);
    }

    public void SetTenantContext(TenantExecutionEnvelope? context)
    {
        _tenantContext = context;
    }

    public string GetCurrentTenantId()
    {
        return GetTenant().TenantId;
    }

    public bool TryGetCurrentTenantId([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out string? tenantId)
    {
        tenantId = _tenantContext?.TenantId;
        return tenantId != null;
    }

    public TenantExecutionEnvelope GetTenant()
    {
        if (_tenantContext is null)
        {
            throw new TenantResolutionException("No tenant context");
        }
        return _tenantContext;
    }

    public bool TryGetTenant(out TenantExecutionEnvelope? tenant)
    {
        tenant = _tenantContext;
        return _tenantContext is not null;
    }
}
