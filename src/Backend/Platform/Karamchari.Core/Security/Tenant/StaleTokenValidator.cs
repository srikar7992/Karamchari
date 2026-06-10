// -----------------------------------------------------------------------
// <copyright file="StaleTokenValidator.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.Extensions.Logging;

namespace Karamchari.Core.Security.Tenant;

public sealed class StaleTokenValidator
{
    private readonly ILogger<StaleTokenValidator> _logger;
    private readonly TimeSpan _tokenExpirationWindow;
    private readonly HashSet<string> _usedTokenIds = new();

    public StaleTokenValidator(
        ILogger<StaleTokenValidator> logger,
        TimeSpan? tokenExpirationWindow = null)
    {
        _logger = logger;
        _tokenExpirationWindow = tokenExpirationWindow ?? TimeSpan.FromHours(1);
    }

    public bool ValidateTokenExpiration(
        string tokenId,
        DateTime tokenIssuedAt,
        DateTime currentTime)
    {
        var tokenAge = currentTime - tokenIssuedAt;

        if (tokenAge > _tokenExpirationWindow)
        {
            _logger.LogWarning(
                "STALE TOKEN: Token {TokenId} has expired (age: {Age}, max: {Max})",
                tokenId,
                tokenAge,
                _tokenExpirationWindow);

            return false;
        }

        return true;
    }

    public bool ValidateTokenExpiration(DateTime tokenIssuedAt)
    {
        return ValidateTokenExpiration(
            string.Empty,
            tokenIssuedAt,
            DateTime.UtcNow);
    }

    public bool RejectStaleToken(string tokenId, DateTime tokenIssuedAt)
    {
        var tokenAge = DateTime.UtcNow - tokenIssuedAt;

        if (tokenAge > _tokenExpirationWindow)
        {
            _logger.LogWarning(
                "STALE TOKEN: Rejecting stale token {TokenId} (age: {Age})",
                tokenId,
                tokenAge);

            return false;
        }

        _logger.LogDebug(
            "STALE TOKEN: Token {TokenId} is valid (age: {Age})",
            tokenId,
            tokenAge);

        return true;
    }

    public bool PreventTokenReuse(string tokenId)
    {
        lock (_usedTokenIds)
        {
            if (_usedTokenIds.Contains(tokenId))
            {
                _logger.LogWarning(
                    "STALE TOKEN: Token reuse detected for {TokenId}",
                    tokenId);

                return false;
            }

            _usedTokenIds.Add(tokenId);
            return true;
        }
    }

    public bool ValidateTokenNotRevoked(string tokenId, IReadOnlyList<string> revokedTokens)
    {
        if (revokedTokens.Contains(tokenId))
        {
            _logger.LogWarning(
                "STALE TOKEN: Token {TokenId} has been revoked",
                tokenId);

            return false;
        }

        return true;
    }

    public StaleTokenReport GetReport()
    {
        lock (_usedTokenIds)
        {
            return new StaleTokenReport(
                _usedTokenIds.Count,
                _tokenExpirationWindow);
        }
    }

    public void Clear()
    {
        lock (_usedTokenIds)
        {
            _usedTokenIds.Clear();
        }
    }

    public TimeSpan GetTokenExpirationWindow() => _tokenExpirationWindow;
}

public sealed record StaleTokenReport(
    int UsedTokenCount,
    TimeSpan ExpirationWindow);
