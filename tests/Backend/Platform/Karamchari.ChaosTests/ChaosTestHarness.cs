// -----------------------------------------------------------------------
// <copyright file="ChaosTestHarness.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Messaging.Outbox;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Karamchari.ChaosTests;

/// <summary>
/// In-process chaos test harness that simulates broker failure scenarios
/// without requiring real RabbitMQ or SQL Server containers.
///
/// Uses a <see cref="FakeBus"/> whose <c>BrokerDown</c> flag can be toggled
/// to simulate connection failures mid-flight, enabling deterministic
/// assertions on outbox and circuit-breaker state.
/// </summary>
public sealed class ChaosTestHarness : IDisposable
{
    private readonly ServiceProvider _provider;

    /// <summary>Gets the in-memory EF context used for outbox state assertions.</summary>
    public OutboxRelayDbContext Db { get; }

    /// <summary>Gets the controllable fake bus. Toggle <see cref="FakeBus.BrokerDown"/> to inject failures.</summary>
    public FakeBus Bus { get; }

    /// <summary>Gets the circuit breaker instance wired into the relay.</summary>
    public OutboxRelayCircuitBreaker CircuitBreaker { get; }

    /// <summary>Gets the relay options used in this test run.</summary>
    public OutboxRelayOptions Options { get; }

    /// <summary>Initializes a new instance of <see cref="ChaosTestHarness"/>.</summary>
    public ChaosTestHarness()
    {
        Options = new OutboxRelayOptions
        {
            BatchSize = 10,
            PollingInterval = TimeSpan.FromMilliseconds(50),
            MaxRetries = 3,
            MaxConcurrency = 2,
            CircuitBreakerFailureThreshold = 3,
            CircuitBreakerResetTimeout = TimeSpan.FromSeconds(5)
        };

        var services = new ServiceCollection();

        services.AddSingleton<IOptions<OutboxRelayOptions>>(
            new OptionsWrapper<OutboxRelayOptions>(Options));

        services.AddDbContext<OutboxRelayDbContext>(opts =>
            opts.UseInMemoryDatabase($"OutboxChaos_{Guid.NewGuid()}"));

        var fakeBus = new FakeBus();
        services.AddSingleton<IBus>(fakeBus.Mock);
        services.AddSingleton(fakeBus);

        services.AddSingleton<OutboxRelayCircuitBreaker>();
        services.AddSingleton<OutboxRelayMetrics>();
        services.AddSingleton<OutboxMessageTypeRegistry>();
        services.AddLogging();

        _provider = services.BuildServiceProvider();

        Db = _provider.GetRequiredService<OutboxRelayDbContext>();
        Bus = fakeBus;
        CircuitBreaker = _provider.GetRequiredService<OutboxRelayCircuitBreaker>();
    }

    /// <summary>
    /// Inserts a fake <see cref="OutboxProcessingState"/> row to represent an
    /// outbox message the relay is responsible for. Returns the new outbox ID.
    /// </summary>
    public async Task<Guid> InsertOutboxStateAsync(CancellationToken ct = default)
    {
        var outboxId = Guid.NewGuid();
        Db.ProcessingStates.Add(new OutboxProcessingState
        {
            OutboxId = outboxId,
            RetryCount = 0,
            LastAttemptUtc = null,
            NextRetryUtc = null,
            LastError = null
        });
        await Db.SaveChangesAsync(ct);
        return outboxId;
    }

    /// <summary>Simulates a broker-down condition: all subsequent publishes throw <see cref="InvalidOperationException"/>.</summary>
    public void SimulateBrokerDown() => Bus.BrokerDown = true;

    /// <summary>Restores normal broker operation.</summary>
    public void SimulateBrokerUp() => Bus.BrokerDown = false;

    /// <summary>
    /// Drives the circuit breaker to the open state by recording the configured
    /// number of consecutive failures.
    /// </summary>
    public void TripCircuitBreaker()
    {
        for (var i = 0; i < Options.CircuitBreakerFailureThreshold; i++)
            CircuitBreaker.RecordFailure();
    }

    /// <summary>Returns the processing state for a given outbox row, or <c>null</c> if absent.</summary>
    public Task<OutboxProcessingState?> GetProcessingStateAsync(Guid outboxId, CancellationToken ct = default)
        => Db.ProcessingStates.FirstOrDefaultAsync(s => s.OutboxId == outboxId, ct);

    /// <summary>Counts dead-letter records in the in-memory store.</summary>
    public Task<int> CountDeadLetterAsync(CancellationToken ct = default)
        => Db.DeadLetterMessages.CountAsync(ct);

    /// <inheritdoc />
    public void Dispose() => _provider.Dispose();
}
