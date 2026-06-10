// -----------------------------------------------------------------------
// <copyright file="ForgedHeaderValidator.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.Extensions.Logging;

namespace Karamchari.Core.Security.Tenant;

public sealed class ForgedHeaderValidator
{
    private readonly ILogger<ForgedHeaderValidator> _logger;
    private readonly HashSet<string> _validHeaders = new()
    {
        "X-Tenant-Id",
        "X-Tenant-ID",
        "x-tenant-id",
        "tenant_id",
        "TenantId"
    };

    public ForgedHeaderValidator(ILogger<ForgedHeaderValidator> logger)
    {
        _logger = logger;
    }

    public bool ValidateHeader(string? headerName, string? headerValue, string? authenticatedTenantId)
    {
        if (string.IsNullOrWhiteSpace(headerName) || string.IsNullOrWhiteSpace(headerValue))
        {
            _logger.LogWarning(
                "FORGED HEADER: Missing header name or value");

            return false;
        }

        var isValidHeader = _validHeaders.Contains(headerName, StringComparer.OrdinalIgnoreCase);

        if (!isValidHeader)
        {
            _logger.LogWarning(
                "FORGED HEADER: Invalid header name {HeaderName}",
                headerName);

            return false;
        }

        var isValidTenant = ValidateTenantIdFormat(headerValue);

        if (!isValidTenant)
        {
            _logger.LogWarning(
                "FORGED HEADER: Invalid tenant ID format in header {HeaderName}: {Value}",
                headerName,
                headerValue);

            return false;
        }

        if (!string.IsNullOrEmpty(authenticatedTenantId))
        {
            var tenantMatches = string.Equals(
                headerValue,
                authenticatedTenantId,
                StringComparison.OrdinalIgnoreCase);

            if (!tenantMatches)
            {
                _logger.LogWarning(
                    "FORGED HEADER: Header tenant {HeaderTenant} does not match authenticated tenant {AuthTenant}",
                    headerValue,
                    authenticatedTenantId);

                return false;
            }
        }

        return true;
    }

    public bool ValidateHeaderTampering(
        string originalHeaderValue,
        string receivedHeaderValue)
    {
        if (string.IsNullOrWhiteSpace(originalHeaderValue) ||
            string.IsNullOrWhiteSpace(receivedHeaderValue))
        {
            return false;
        }

        var tampered = !string.Equals(
            originalHeaderValue,
            receivedHeaderValue,
            StringComparison.OrdinalIgnoreCase);

        if (tampered)
        {
            _logger.LogWarning(
                "FORGED HEADER: Header tampering detected - original: {Original}, received: {Received}",
                originalHeaderValue,
                receivedHeaderValue);
        }

        return !tampered;
    }

    public bool ValidateMissingHeader(string? headerValue, bool allowMissing = false)
    {
        if (string.IsNullOrWhiteSpace(headerValue))
        {
            if (allowMissing)
            {
                _logger.LogDebug(
                    "FORGED HEADER: Missing header allowed");

                return true;
            }

            _logger.LogWarning(
                "FORGED HEADER: Required header is missing");

            return false;
        }

        return true;
    }

    public bool ValidateTenantIdFormat(string? tenantId)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            return false;
        }

        if (tenantId.Length > 50)
        {
            _logger.LogWarning(
                "FORGED HEADER: Tenant ID too long: {Length}",
                tenantId.Length);

            return false;
        }

        foreach (var c in tenantId)
        {
            if (!char.IsLetterOrDigit(c) && c != '-' && c != '_')
            {
                _logger.LogWarning(
                    "FORGED HEADER: Invalid character in tenant ID: {Char}",
                    c);

                return false;
            }
        }

        return true;
    }

    public bool ValidateCrossTenantHeader(string? headerTenantId, string? authenticatedTenantId)
    {
        if (string.IsNullOrWhiteSpace(headerTenantId))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(authenticatedTenantId))
        {
            return false;
        }

        var isCrossTenant = !string.Equals(
            headerTenantId,
            authenticatedTenantId,
            StringComparison.OrdinalIgnoreCase);

        if (isCrossTenant)
        {
            _logger.LogWarning(
                "FORGED HEADER: Cross-tenant header manipulation detected - header: {HeaderTenant}, auth: {AuthTenant}",
                headerTenantId,
                authenticatedTenantId);
        }

        return !isCrossTenant;
    }

    public IReadOnlyList<string> GetValidHeaders() => _validHeaders.ToList().AsReadOnly();
}

public sealed record HeaderValidationResult(
    bool IsValid,
    string? Error,
    string HeaderName,
    string HeaderValue);
