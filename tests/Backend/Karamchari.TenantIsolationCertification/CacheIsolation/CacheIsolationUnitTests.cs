using FluentAssertions;
using Karamchari.Core.Caching.Tenant;
using Karamchari.TenantIsolationCertification.Infrastructure;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Karamchari.TenantIsolationCertification.CacheIsolation;

public sealed class TenantCacheNamespaceTests
{
    [Theory]
    [InlineData("acme", "employees", "tenant_acme:employees")]
    [InlineData("globex", "payroll", "tenant_globex:payroll")]
    [InlineData("initech", "data", "tenant_initech:data")]
    public void Build_WithValidInputs_ShouldReturnNamespacedKey(string tenantId, string key, string expected)
    {
        var result = TenantCacheNamespace.Build(tenantId, key);

        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("acme", "employees", "attendance", "tenant_acme:employees:attendance")]
    [InlineData("globex", "payroll", "2024", "tenant_globex:payroll:2024")]
    public void Build_WithCategory_ShouldIncludeCategoryInKey(string tenantId, string category, string key, string expected)
    {
        var result = TenantCacheNamespace.Build(tenantId, category, key);

        result.Should().Be(expected);
    }

    [Fact]
    public void Build_WithUppercaseTenantId_ShouldNormalizeToLowercase()
    {
        var result = TenantCacheNamespace.Build("ACME", "employees");

        result.Should().StartWith("tenant_acme:");
    }

    [Fact]
    public void Build_WithWhitespaceInKey_ShouldNormalize()
    {
        var result = TenantCacheNamespace.Build("acme", "employee data");

        result.Should().Contain("employee_data");
    }

    [Theory]
    [InlineData("tenant_acme:employees")]
    [InlineData("tenant_globex:payroll")]
    [InlineData("tenant_initech:data")]
    public void IsTenantKey_WithValidTenantKey_ShouldReturnTrue(string key)
    {
        var result = TenantCacheNamespace.IsTenantKey(key);

        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("acme:employees")]
    [InlineData("tenant:acme:employees")]
    [InlineData("cache_key")]
    [InlineData("something_else")]
    public void IsTenantKey_WithInvalidKey_ShouldReturnFalse(string key)
    {
        var result = TenantCacheNamespace.IsTenantKey(key);

        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("tenant_acme:employees", "acme", "employees")]
    [InlineData("tenant_globex:payroll:2024", "globex", "payroll:2024")]
    public void Parse_WithValidKey_ShouldReturnTenantAndKey(string key, string expectedTenantId, string expectedKey)
    {
        var (tenantId, parsedKey) = TenantCacheNamespace.Parse(key);

        tenantId.Should().Be(expectedTenantId);
        parsedKey.Should().Be(expectedKey);
    }

    [Fact]
    public void Parse_WithInvalidKey_ShouldThrowArgumentException()
    {
        var action = () => TenantCacheNamespace.Parse("invalid_key");

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Build_WithUnicodeNormalization_ShouldNormalizeKeys()
    {
        var result = TenantCacheNamespace.Build("acme", "café");

        result.Should().Be("tenant_acme:caf");
    }

    [Fact]
    public void Build_WithKeyTooLong_ShouldThrowArgumentException()
    {
        var longKey = new string('a', 200);

        var action = () => TenantCacheNamespace.Build("acme", longKey);

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Build_WithInvalidTenantIdFormat_ShouldThrowArgumentException()
    {
        var action = () => TenantCacheNamespace.Build("acme!@#", "employees");

        action.Should().Throw<ArgumentException>();
    }
}

public sealed class TenantCacheKeyBuilderTests
{
    [Fact]
    public void Build_WithValidInputs_ShouldReturnCorrectKey()
    {
        var builder = new TenantCacheKeyBuilder();

        var result = builder.Build("acme", "employees");

        result.Should().Be("tenant_acme:employees");
    }

    [Fact]
    public void ValidateTenantId_WithValidId_ShouldNotThrow()
    {
        var builder = new TenantCacheKeyBuilder();

        var action = () => builder.ValidateTenantId("acme");

        action.Should().NotThrow();
    }

    [Fact]
    public void ValidateTenantId_WithInvalidCharacters_ShouldThrow()
    {
        var builder = new TenantCacheKeyBuilder();

        var action = () => builder.ValidateTenantId("acme!@#");

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ValidateTenantId_WithConfusableUnicode_ShouldThrow()
    {
        var builder = new TenantCacheKeyBuilder();

        var action = () => builder.ValidateTenantId("acme\u200b");

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ValidateKey_WithValidKey_ShouldNotThrow()
    {
        var builder = new TenantCacheKeyBuilder();

        var action = () => builder.ValidateKey("employees");

        action.Should().NotThrow();
    }

    [Fact]
    public void ValidateKey_WithControlCharacters_ShouldThrow()
    {
        var builder = new TenantCacheKeyBuilder();

        var action = () => builder.ValidateKey("employ\x00ees");

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ValidateKey_WithTrailingWhitespace_ShouldThrow()
    {
        var builder = new TenantCacheKeyBuilder();

        var action = () => builder.ValidateKey("employees ");

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ValidateKey_WithKeyTooLong_ShouldThrow()
    {
        var builder = new TenantCacheKeyBuilder();
        var longKey = new string('a', 300);

        var action = () => builder.ValidateKey(longKey);

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void TryBuild_WithValidInputs_ShouldReturnTrue()
    {
        var builder = new TenantCacheKeyBuilder();

        var success = builder.TryBuild("acme", "employees", out var result);

        success.Should().BeTrue();
        result.Should().Be("tenant_acme:employees");
    }

    [Fact]
    public void TryBuild_WithInvalidInputs_ShouldReturnFalse()
    {
        var builder = new TenantCacheKeyBuilder();

        var success = builder.TryBuild("acme!", "employees", out var result);

        success.Should().BeFalse();
        result.Should().BeNull();
    }
}

public sealed class TenantCacheValidatorTests
{
    [Fact]
    public void Validate_WithValidKey_ShouldReturnValidResult()
    {
        var validator = new TenantCacheValidator();

        var result = validator.Validate("tenant_acme:employees");

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithNullKey_ShouldReturnInvalidResult()
    {
        var validator = new TenantCacheValidator();

        var result = validator.Validate(null!);

        result.IsValid.Should().BeFalse();
        result.Error.Should().Be(TenantCacheValidationError.NullOrEmpty);
    }

    [Fact]
    public void Validate_WithMissingTenantPrefix_ShouldReturnInvalidResult()
    {
        var validator = new TenantCacheValidator();

        var result = validator.Validate("acme:employees");

        result.IsValid.Should().BeFalse();
        result.Error.Should().Be(TenantCacheValidationError.MissingTenantPrefix);
    }

    [Fact]
    public void Validate_WithUnicodeConfusable_ShouldReturnInvalidResult()
    {
        var validator = new TenantCacheValidator();

        var result = validator.Validate("tenant_acme\u200b:employees");

        result.IsValid.Should().BeFalse();
        result.Error.Should().Be(TenantCacheValidationError.UnicodeConfusable);
    }

    [Fact]
    public void Validate_WithKeyInjection_ShouldReturnInvalidResult()
    {
        var validator = new TenantCacheValidator();

        var result = validator.Validate("tenant_acme:tenant_globex:employees");

        result.IsValid.Should().BeFalse();
        result.Error.Should().Be(TenantCacheValidationError.KeyInjection);
    }

    [Fact]
    public void ValidateWithDiagnostics_ShouldAddDiagnosticInfo()
    {
        var validator = new TenantCacheValidator();

        var result = validator.ValidateWithDiagnostics("tenant_acme:employees");

        result.Diagnostics.Should().NotBeEmpty();
    }

    [Fact]
    public void GenerateDiagnosticsReport_WithValidKey_ShouldContainKeyDetails()
    {
        var validator = new TenantCacheValidator();

        var report = validator.GenerateDiagnosticsReport("tenant_acme:employees");

        report.Should().Contain("tenant_acme");
        report.Should().Contain("employees");
    }

    [Fact]
    public void GenerateDiagnosticsReport_WithInvalidKey_ShouldContainErrorDetails()
    {
        var validator = new TenantCacheValidator();

        var report = validator.GenerateDiagnosticsReport("invalid_key");

        report.Should().Contain("INVALID");
    }
}

public sealed class TenantCacheGuardTests
{
    [Fact]
    public void ValidateGet_WithMatchingTenant_ShouldNotThrow()
    {
        var ctx = TenantTestContext.Create("acme", TenantSource.JwtClaim);
        var accessor = new TestTenantContextAccessor(ctx);
        var guard = new TenantCacheGuard(accessor);

        var action = () => guard.ValidateGet("tenant_acme:employees");

        action.Should().NotThrow();
    }

    [Fact]
    public void ValidateGet_WithMismatchedTenant_ShouldThrowCachePoisoningException()
    {
        var ctx = TenantTestContext.Create("globex", TenantSource.JwtClaim);
        var accessor = new TestTenantContextAccessor(ctx);
        var guard = new TenantCacheGuard(accessor);

        var action = () => guard.ValidateGet("tenant_acme:employees");

        action.Should().Throw<CachePoisoningDetectionException>();
    }

    [Fact]
    public void ValidateGet_WithoutTenantContext_ShouldThrowInvalidOperationException()
    {
        var accessor = new TestTenantContextAccessor(null);
        var guard = new TenantCacheGuard(accessor);

        var action = () => guard.ValidateGet("tenant_acme:employees");

        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ValidateGet_WithNonTenantKey_ShouldThrowArgumentException()
    {
        var ctx = TenantTestContext.Create("acme", TenantSource.JwtClaim);
        var accessor = new TestTenantContextAccessor(ctx);
        var guard = new TenantCacheGuard(accessor);

        var action = () => guard.ValidateGet("invalid_key");

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ValidateSet_WithMatchingTenant_ShouldNotThrow()
    {
        var ctx = TenantTestContext.Create("acme", TenantSource.JwtClaim);
        var accessor = new TestTenantContextAccessor(ctx);
        var guard = new TenantCacheGuard(accessor);

        var action = () => guard.ValidateSet("tenant_acme:employees");

        action.Should().NotThrow();
    }

    [Fact]
    public void ValidateSet_WithMismatchedTenant_ShouldThrowCachePoisoningException()
    {
        var ctx = TenantTestContext.Create("globex", TenantSource.JwtClaim);
        var accessor = new TestTenantContextAccessor(ctx);
        var guard = new TenantCacheGuard(accessor);

        var action = () => guard.ValidateSet("tenant_acme:employees");

        action.Should().Throw<CachePoisoningDetectionException>();
    }

    [Fact]
    public void TryValidateGet_WithValidKey_ShouldReturnTrue()
    {
        var ctx = TenantTestContext.Create("acme", TenantSource.JwtClaim);
        var accessor = new TestTenantContextAccessor(ctx);
        var guard = new TenantCacheGuard(accessor);

        var success = guard.TryValidateGet("tenant_acme:employees");

        success.Should().BeTrue();
    }

    [Fact]
    public void TryValidateGet_WithInvalidKey_ShouldReturnFalse()
    {
        var ctx = TenantTestContext.Create("acme", TenantSource.JwtClaim);
        var accessor = new TestTenantContextAccessor(ctx);
        var guard = new TenantCacheGuard(accessor);

        var success = guard.TryValidateGet("invalid_key");

        success.Should().BeFalse();
    }

    [Fact]
    public void BuildValidKey_ShouldReturnTenantNamespacedKey()
    {
        var ctx = TenantTestContext.Create("acme", TenantSource.JwtClaim);
        var accessor = new TestTenantContextAccessor(ctx);
        var guard = new TenantCacheGuard(accessor);

        var key = guard.BuildValidKey("employees");

        key.Should().Be("tenant_acme:employees");
    }

    [Fact]
    public void BuildValidKey_WithCategory_ShouldReturnCategoryKey()
    {
        var ctx = TenantTestContext.Create("acme", TenantSource.JwtClaim);
        var accessor = new TestTenantContextAccessor(ctx);
        var guard = new TenantCacheGuard(accessor);

        var key = guard.BuildValidKey("payroll", "employees");

        key.Should().Be("tenant_acme:payroll:employees");
    }
}

public sealed class CachePoisoningDetectionExceptionTests
{
    [Fact]
    public void Exception_ShouldStoreDetectionTime()
    {
        var ex = new CachePoisoningDetectionException("test");

        ex.DetectionTimeUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Exception_WithAttackKey_ShouldStoreKey()
    {
        var ex = new CachePoisoningDetectionException("test", "malicious_key");

        ex.AttackKey.Should().Be("malicious_key");
    }

    [Fact]
    public void Exception_DefaultConstructor_ShouldHaveNullAttackKey()
    {
        var ex = new CachePoisoningDetectionException("test");

        ex.AttackKey.Should().BeNull();
    }
}

public sealed class TenantCacheValidationResultTests
{
    [Fact]
    public void MarkInvalid_ShouldSetErrorAndInvalidFlag()
    {
        var result = new TenantCacheValidationResult();

        result.MarkInvalid(TenantCacheValidationError.NullOrEmpty, "Test error");

        result.IsValid.Should().BeFalse();
        result.Error.Should().Be(TenantCacheValidationError.NullOrEmpty);
        result.ErrorMessage.Should().Be("Test error");
    }

    [Fact]
    public void HasError_WhenErrorSet_ShouldReturnTrue()
    {
        var result = new TenantCacheValidationResult();
        result.MarkInvalid(TenantCacheValidationError.NullOrEmpty, "Error");

        result.HasError.Should().BeTrue();
    }

    [Fact]
    public void ThrowIfInvalid_WhenInvalid_ShouldThrowException()
    {
        var result = new TenantCacheValidationResult();
        result.MarkInvalid(TenantCacheValidationError.NullOrEmpty, "Error");

        var action = () => result.ThrowIfInvalid();

        action.Should().Throw<TenantCacheValidationException>();
    }

    [Fact]
    public void ThrowIfInvalid_WhenValid_ShouldNotThrow()
    {
        var result = new TenantCacheValidationResult();

        var action = () => result.ThrowIfInvalid();

        action.Should().NotThrow();
    }

    [Fact]
    public void AddDiagnostic_ShouldAddToDiagnosticsList()
    {
        var result = new TenantCacheValidationResult();
        result.AddDiagnostic("Test diagnostic");

        result.Diagnostics.Should().Contain("Test diagnostic");
    }
}

internal sealed class TestTenantContextAccessor : ITenantContextAccessor
{
    private readonly TenantContext? _context;

    public TestTenantContextAccessor(TenantTestContext? context)
    {
        _context = context == null
            ? null
            : new TenantContext
            {
                TenantId = context.ActiveTenantId,
                TenantSchema = context.ActiveSchema,
                Source = context.ActiveSource
            };
    }

    public TenantContext? TenantContext => _context;
}
