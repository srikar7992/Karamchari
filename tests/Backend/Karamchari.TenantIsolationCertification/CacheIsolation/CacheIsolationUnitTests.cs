// -----------------------------------------------------------------------
// <copyright file="CacheIsolationUnitTests.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Text;
using FluentAssertions;
using Karamchari.Core.Caching.Tenant;
using Karamchari.Core.Multitenancy;
using Karamchari.TenantIsolationCertification.Infrastructure;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Karamchari.TenantIsolationCertification.CacheIsolation;

public sealed class TenantCacheGuardTests : IDisposable
{
    private readonly TenantTestContext _context;
    private readonly Mock<ITenantProvider> _tenantProviderMock;
    private readonly TenantCacheGuard _guard;

    public TenantCacheGuardTests()
    {
        _context = TenantTestContext.Create("acme");
        _tenantProviderMock = new Mock<ITenantProvider>();
        _tenantProviderMock.Setup(p => p.GetTenant()).Returns(new TenantExecutionEnvelope("acme", "corr-123", "req-123", ExecutionSource.Test, TenantSource.JwtClaim));

        _guard = new TenantCacheGuard(_tenantProviderMock.Object);
    }

    [Fact]
    public void ValidateGet_WithMatchingTenant_ShouldNotThrow()
    {
        var key = TenantCacheNamespace.Build("acme", "test-key");

        var action = () => _guard.ValidateGet(key);

        action.Should().NotThrow();
    }

    [Fact]
    public void ValidateGet_WithMismatchingTenant_ShouldThrow()
    {
        var key = TenantCacheNamespace.Build("globex", "test-key");

        var action = () => _guard.ValidateGet(key);

        action.Should().Throw<CachePoisoningDetectionException>();
    }

    [Fact]
    public void ValidateGet_WithNonTenantKey_ShouldThrow()
    {
        var key = "global_config_key";

        var action = () => _guard.ValidateGet(key);

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ValidateSet_WithMatchingTenant_ShouldNotThrow()
    {
        var key = TenantCacheNamespace.Build("acme", "test-key");

        var action = () => _guard.ValidateSet(key);

        action.Should().NotThrow();
    }

    [Fact]
    public void ValidateSet_WithMismatchingTenant_ShouldThrow()
    {
        var key = TenantCacheNamespace.Build("globex", "test-key");

        var action = () => _guard.ValidateSet(key);

        action.Should().Throw<CachePoisoningDetectionException>();
    }

    [Fact]
    public void TryValidateGet_ShouldReturnCorrectResult()
    {
        var validKey = TenantCacheNamespace.Build("acme", "test-key");
        var invalidKey = TenantCacheNamespace.Build("globex", "test-key");

        _guard.TryValidateGet(validKey).Should().BeTrue();
        _guard.TryValidateGet(invalidKey).Should().BeFalse();
    }

    [Fact]
    public void BuildValidKey_ShouldUseCurrentTenant()
    {
        var key = _guard.BuildValidKey("my-data");

        key.Should().StartWith("tenant_acme:");
        key.Should().Contain("my-data");
    }

    [Fact]
    public void BuildValidKey_WithCategory_ShouldUseCurrentTenant()
    {
        var key = _guard.BuildValidKey("security", "my-data");

        key.Should().StartWith("tenant_acme:");
        key.Should().Contain("security");
        key.Should().Contain("my-data");
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}

public sealed class TenantCacheKeyBuilderTests
{
    private readonly TenantCacheKeyBuilder _builder;

    public TenantCacheKeyBuilderTests()
    {
        _builder = new TenantCacheKeyBuilder();
    }

    [Fact]
    public void Build_ValidInput_ReturnsCorrectKey()
    {
        var result = _builder.Build("acme", "user_123");

        result.Should().Be("tenant_acme:user_123");
    }

    [Fact]
    public void Build_WithCategory_ReturnsCorrectKey()
    {
        var result = _builder.Build("acme", "settings", "theme");

        result.Should().Be("tenant_acme:settings:theme");
    }

    [Fact]
    public void Build_NormalizesTenantId()
    {
        var result = _builder.Build("ACME ", "key");

        result.Should().Be("tenant_acme:key");
    }

    [Fact]
    public void Build_NormalizesKey()
    {
        var result = _builder.Build("acme", " user profile ");

        result.Should().Be("tenant_acme:user_profile");
    }

    [Fact]
    public void Build_InvalidTenantId_ThrowsArgumentException()
    {
        var action = () => _builder.Build("acme!", "key");

        action.Should().Throw<ArgumentException>().WithMessage("*Tenant ID*");
    }

    [Fact]
    public void Build_TenantIdTooLong_ThrowsArgumentException()
    {
        var longId = new string('a', 65);
        var action = () => _builder.Build(longId, "key");

        action.Should().Throw<ArgumentException>().WithMessage("*exceeds maximum length*");
    }

    [Fact]
    public void Build_KeyTooLong_ThrowsArgumentException()
    {
        var longKey = new string('k', 257);
        var action = () => _builder.Build("acme", longKey);

        action.Should().Throw<ArgumentException>().WithMessage("*Key exceeds maximum length*");
    }

    [Fact]
    public void Build_KeyWithInvalidChars_ThrowsArgumentException()
    {
        var action = () => _builder.Build("acme", "key\nwith\nnewline");

        action.Should().Throw<ArgumentException>().WithMessage("*invalid control character*");
    }

    [Theory]
    [InlineData("\u200b")] // Zero-width space
    [InlineData("\ufeff")] // Byte order mark
    public void Build_WithConfusableUnicode_ThrowsArgumentException(string confusable)
    {
        var action = () => _builder.Build("acme" + confusable, "key");

        action.Should().Throw<ArgumentException>();
    }
}

public sealed class TenantCacheNamespaceTests
{
    [Fact]
    public void Build_ValidInput_ReturnsExpectedString()
    {
        var result = TenantCacheNamespace.Build("acme", "key");

        result.Should().Be("tenant_acme:key");
    }

    [Fact]
    public void Build_WithCategory_ReturnsExpectedString()
    {
        var result = TenantCacheNamespace.Build("acme", "cat", "key");

        result.Should().Be("tenant_acme:cat:key");
    }

    [Fact]
    public void IsTenantKey_CorrectPrefix_ReturnsTrue()
    {
        TenantCacheNamespace.IsTenantKey("tenant_acme:key").Should().BeTrue();
    }

    [Fact]
    public void IsTenantKey_WrongPrefix_ReturnsFalse()
    {
        TenantCacheNamespace.IsTenantKey("global_key").Should().BeFalse();
    }

    [Fact]
    public void Parse_ValidKey_ReturnsParts()
    {
        var (tenantId, key) = TenantCacheNamespace.Parse("tenant_acme:my:data:key");

        tenantId.Should().Be("acme");
        key.Should().Be("my:data:key");
    }

    [Fact]
    public void Parse_InvalidKey_ThrowsArgumentException()
    {
        var action = () => TenantCacheNamespace.Parse("invalid_key");

        action.Should().Throw<ArgumentException>();
    }
}

public sealed class TenantCacheValidatorTests
{
    private readonly TenantCacheValidator _validator;

    public TenantCacheValidatorTests()
    {
        _validator = new TenantCacheValidator();
    }

    [Fact]
    public void Validate_ValidKey_ReturnsValid()
    {
        var result = _validator.Validate("tenant_acme:user_profile");

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_MissingPrefix_ReturnsInvalid()
    {
        var result = _validator.Validate("acme:user_profile");

        result.IsValid.Should().BeFalse();
        result.Error.Should().Be(TenantCacheValidationError.MissingTenantPrefix);
    }

    [Fact]
    public void Validate_InvalidTenantFormat_ReturnsInvalid()
    {
        var result = _validator.Validate("tenant_ACME_123:key");

        result.IsValid.Should().BeFalse();
        result.Error.Should().Be(TenantCacheValidationError.InvalidTenantIdFormat);
    }

    [Fact]
    public void Validate_EmptyKey_ReturnsInvalid()
    {
        var result = _validator.Validate("tenant_acme:");

        result.IsValid.Should().BeFalse();
        result.Error.Should().Be(TenantCacheValidationError.InvalidTenantIdFormat);
    }

    [Fact]
    public void Validate_KeyInjectionAttempt_ReturnsInvalid()
    {
        // Attempting to inject another tenant prefix via the key
        var result = _validator.Validate("tenant_acme:sub:tenant_globex:data");

        result.IsValid.Should().BeFalse();
        result.Error.Should().Be(TenantCacheValidationError.KeyInjection);
    }

    [Fact]
    public void Validate_UnicodeCollision_ReturnsInvalid()
    {
        // Using a character that might normalize or collide suspiciously
        // Note: The actual implementation of DetectUnicodeCollision uses some heuristics
        // that we're testing here.
        var result = _validator.Validate("tenant_acme\u200b:key");

        result.IsValid.Should().BeFalse();
        result.Error.Should().Be(TenantCacheValidationError.InvalidTenantIdFormat);
    }

    [Fact]
    public void GenerateDiagnosticsReport_ReturnsDetailedString()
    {
        var report = _validator.GenerateDiagnosticsReport("invalid-key");

        report.Should().Contain("INVALID");
        report.Should().Contain("Error Type");
        report.Should().Contain("Diagnostics");
    }

    [Fact]
    public void ThrowIfInvalid_WithInvalidResult_ThrowsException()
    {
        var result = _validator.Validate("bad-key");

        var action = () => result.ThrowIfInvalid();

        action.Should().Throw<TenantCacheValidationException>();
    }

    [Fact]
    public void ThrowIfInvalid_WithValidResult_DoesNotThrow()
    {
        var result = _validator.Validate("tenant_acme:valid");

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
