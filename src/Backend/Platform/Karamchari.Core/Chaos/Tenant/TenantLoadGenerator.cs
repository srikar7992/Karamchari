// -----------------------------------------------------------------------
// <copyright file="TenantLoadGenerator.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Karamchari.Core.Chaos.Tenant;

public sealed partial class TenantLoadGenerator
{
    private readonly ILogger<TenantLoadGenerator> _logger;
    private readonly TenantChaosEngine _chaosEngine;
    private static readonly Random _random = new();
    private bool _isRunning;

    public int ActiveTenantCount { get; private set; }
    public long TotalOperationsCompleted { get; private set; }
    public long PayrollOperationsCompleted { get; private set; }
    public long AttendanceOperationsCompleted { get; private set; }

    public TenantLoadGenerator(
        ILogger<TenantLoadGenerator> logger,
        TenantChaosEngine chaosEngine)
    {
        _logger = logger;
        _chaosEngine = chaosEngine;
    }

    public async Task GenerateLoadAsync(int tenantCount, TimeSpan duration)
    {
        if (_isRunning)
        {
            throw new InvalidOperationException("Load generation is already running");
        }

        _isRunning = true;
        ActiveTenantCount = tenantCount;

        var tenants = GenerateTenantIds(tenantCount);
        var endTime = DateTime.UtcNow + duration;
        var operationCount = 0;

        LogStartingLoadGeneration(_logger, tenantCount, duration);

        try
        {
            while (DateTime.UtcNow < endTime)
            {
                var tasks = new List<Task>();

                foreach (var tenant in tenants)
                {
                    var operationType = operationCount % 4;

                    if (operationType is 0 or 1)
                    {
                        tasks.Add(ExecutePayrollOperationAsync(tenant, operationCount));
                    }
                    else
                    {
                        tasks.Add(ExecuteAttendanceOperationAsync(tenant, operationCount));
                    }

                    operationCount++;
                }

                await Task.WhenAll(tasks);

                await Task.Delay(_random.Next(10, 50));
            }
        }
        finally
        {
            _isRunning = false;
        }

        LogLoadGenerationCompleted(_logger, TotalOperationsCompleted, PayrollOperationsCompleted, AttendanceOperationsCompleted);
    }

    public async Task GenerateConcurrentPayrollLoadAsync(
        IReadOnlyList<string> tenants,
        int operationsPerTenant)
    {
        LogStartingConcurrentPayrollLoad(_logger, tenants.Count, operationsPerTenant);

        var semaphore = new SemaphoreSlim(100);
        var tasks = new List<Task>();

        foreach (var tenant in tenants)
        {
            for (int i = 0; i < operationsPerTenant; i++)
            {
                await semaphore.WaitAsync();

                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        await ExecutePayrollOperationAsync(tenant, i);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }));
            }
        }

        await Task.WhenAll(tasks);

        LogConcurrentPayrollLoadCompleted(_logger);
    }

    public async Task GenerateConcurrentAttendanceLoadAsync(
        IReadOnlyList<string> tenants,
        int operationsPerTenant)
    {
        LogStartingConcurrentAttendanceLoad(_logger, tenants.Count, operationsPerTenant);

        var semaphore = new SemaphoreSlim(100);
        var tasks = new List<Task>();

        foreach (var tenant in tenants)
        {
            for (int i = 0; i < operationsPerTenant; i++)
            {
                await semaphore.WaitAsync();

                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        await ExecuteAttendanceOperationAsync(tenant, i);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }));
            }
        }

        await Task.WhenAll(tasks);

        LogConcurrentAttendanceLoadCompleted(_logger);
    }

    public async Task GenerateChaosLoadAsync(
        IReadOnlyList<string> tenants,
        LoadScale scale,
        TimeSpan duration)
    {
        var tenantCount = (int)scale;
        LogGeneratingChaosLoad(_logger, scale, tenantCount);

        await GenerateLoadAsync(Math.Min(tenantCount, tenants.Count), duration);
    }

    private async Task ExecutePayrollOperationAsync(string tenantId, int operationId)
    {
        try
        {
            var chaosType = GetRandomChaosType();
            if (chaosType.HasValue)
            {
                await InjectChaosAsync(tenantId, chaosType.Value);
            }

            var processingTime = TimeSpan.FromMilliseconds(_random.Next(10, 100));
            await Task.Delay(processingTime);

            TotalOperationsCompleted++;
            PayrollOperationsCompleted++;

            LogPayrollOperationCompleted(_logger, operationId, tenantId);
        }
        catch (Exception ex)
        {
            LogPayrollOperationFailed(_logger, ex, operationId, tenantId);
        }
    }

    private async Task ExecuteAttendanceOperationAsync(string tenantId, int operationId)
    {
        try
        {
            var chaosType = GetRandomChaosType();
            if (chaosType.HasValue)
            {
                await InjectChaosAsync(tenantId, chaosType.Value);
            }

            var processingTime = TimeSpan.FromMilliseconds(_random.Next(5, 50));
            await Task.Delay(processingTime);

            TotalOperationsCompleted++;
            AttendanceOperationsCompleted++;

            LogAttendanceOperationCompleted(_logger, operationId, tenantId);
        }
        catch (Exception ex)
        {
            LogAttendanceOperationFailed(_logger, ex, operationId, tenantId);
        }
    }

    private async Task InjectChaosAsync(string tenantId, ChaosType chaosType)
    {
        switch (chaosType)
        {
            case ChaosType.RandomLatency:
                await _chaosEngine.InjectRandomLatencyAsync(
                    tenantId,
                    TimeSpan.FromMilliseconds(10),
                    TimeSpan.FromMilliseconds(100));
                break;

            case ChaosType.PacketLoss:
                await _chaosEngine.InjectPacketLossAsync(
                    tenantId,
                    _random.NextDouble() * 10);
                break;

            case ChaosType.BrokerDelay:
                await _chaosEngine.InjectBrokerDelayAsync(
                    tenantId,
                    TimeSpan.FromMilliseconds(_random.Next(50, 200)));
                break;
        }
    }

    private ChaosType? GetRandomChaosType()
    {
        if (_random.NextDouble() > 0.2)
        {
            return null;
        }

        var chaosTypes = Enum.GetValues<ChaosType>();
        return chaosTypes[_random.Next(chaosTypes.Length)];
    }

    private static IReadOnlyList<string> GenerateTenantIds(int count)
    {
        var prefixes = new[] { "acme", "globex", "initech", "umbrella", "waystar", "stanford", "pector", "levy", "peaky", "crowbar" };
        var tenants = new List<string>();

        for (int i = 0; i < count; i++)
        {
            var prefix = prefixes[i % prefixes.Length];
            tenants.Add($"{prefix}_{i / prefixes.Length + 1}");
        }

        return tenants.AsReadOnly();
    }

    private enum ChaosType
    {
        None,
        RandomLatency,
        PacketLoss,
        BrokerDelay
    }

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Information,
        Message = "Starting load generation for {TenantCount} tenants for {Duration}")]
    static partial void LogStartingLoadGeneration(ILogger logger, int tenantCount, TimeSpan duration);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Information,
        Message = "Load generation completed. Total operations: {Total}, Payroll: {Payroll}, Attendance: {Attendance}")]
    static partial void LogLoadGenerationCompleted(ILogger logger, long total, long payroll, long attendance);

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Information,
        Message = "Starting concurrent payroll load for {TenantCount} tenants, {OperationsPerTenant} operations each")]
    static partial void LogStartingConcurrentPayrollLoad(ILogger logger, int tenantCount, int operationsPerTenant);

    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Information,
        Message = "Concurrent payroll load completed")]
    static partial void LogConcurrentPayrollLoadCompleted(ILogger logger);

    [LoggerMessage(
        EventId = 5,
        Level = LogLevel.Information,
        Message = "Starting concurrent attendance load for {TenantCount} tenants, {OperationsPerTenant} operations each")]
    static partial void LogStartingConcurrentAttendanceLoad(ILogger logger, int tenantCount, int operationsPerTenant);

    [LoggerMessage(
        EventId = 6,
        Level = LogLevel.Information,
        Message = "Concurrent attendance load completed")]
    static partial void LogConcurrentAttendanceLoadCompleted(ILogger logger);

    [LoggerMessage(
        EventId = 7,
        Level = LogLevel.Information,
        Message = "Generating chaos load at scale {Scale} ({TenantCount} tenants)")]
    static partial void LogGeneratingChaosLoad(ILogger logger, LoadScale scale, int tenantCount);

    [LoggerMessage(
        EventId = 8,
        Level = LogLevel.Debug,
        Message = "Payroll operation {OperationId} completed for tenant {TenantId}")]
    static partial void LogPayrollOperationCompleted(ILogger logger, int operationId, string tenantId);

    [LoggerMessage(
        EventId = 9,
        Level = LogLevel.Error,
        Message = "Payroll operation {OperationId} failed for tenant {TenantId}")]
    static partial void LogPayrollOperationFailed(ILogger logger, Exception ex, int operationId, string tenantId);

    [LoggerMessage(
        EventId = 10,
        Level = LogLevel.Debug,
        Message = "Attendance operation {OperationId} completed for tenant {TenantId}")]
    static partial void LogAttendanceOperationCompleted(ILogger logger, int operationId, string tenantId);

    [LoggerMessage(
        EventId = 11,
        Level = LogLevel.Error,
        Message = "Attendance operation {OperationId} failed for tenant {TenantId}")]
    static partial void LogAttendanceOperationFailed(ILogger logger, Exception ex, int operationId, string tenantId);
}

public enum LoadScale
{
    Small = 1,
    Medium = 10,
    Large = 100,
    Extreme = 1000
}
