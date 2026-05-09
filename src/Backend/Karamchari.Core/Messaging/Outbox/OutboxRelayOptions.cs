using System.ComponentModel.DataAnnotations;

namespace Karamchari.Core.Messaging.Outbox;

/// <summary>
/// Configuration for the production outbox relay service that drains
/// <c>dbo.OutboxMessage</c> to Azure Service Bus.
/// </summary>
public sealed class OutboxRelayOptions
{
    /// <summary>IConfiguration section key.</summary>
    public const string SectionName = "OutboxRelay";

    /// <summary>Messages claimed per polling cycle. 50â€“200 is typical for ASB throughput.</summary>
    [Range(1, 1000)]
    public int BatchSize { get; init; } = 100;

    /// <summary>How long the relay sleeps between cycles when nothing is pending.</summary>
    public TimeSpan PollingInterval { get; init; } = TimeSpan.FromSeconds(5);

    /// <summary>Max publish attempts before a message is moved to <c>dbo.OutboxDeadLetter</c>.</summary>
    [Range(1, 20)]
    public int MaxRetries { get; init; } = 5;

    /// <summary>Max parallel message-publishes within one batch.</summary>
    [Range(1, 32)]
    public int MaxConcurrency { get; init; } = 4;

    /// <summary>Base delay for exponential backoff on transient publish failures.</summary>
    public TimeSpan InitialRetryDelay { get; init; } = TimeSpan.FromSeconds(2);

    /// <summary>
    /// <c>OutboxProcessingState</c> rows with a non-null <c>LockAcquiredUtc</c> older than
    /// this threshold are assumed to be from a crashed relay instance and are released on startup.
    /// Must be significantly longer than one relay cycle to avoid self-interference.
    /// </summary>
    public TimeSpan StaleLockTimeout { get; init; } = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Number of consecutive Service Bus publish failures before the circuit opens.
    /// While open, the relay skips batch cycles to avoid hammering a degraded bus.
    /// </summary>
    [Range(1, 100)]
    public int CircuitBreakerFailureThreshold { get; init; } = 5;

    /// <summary>
    /// How long the circuit stays open before transitioning to HalfOpen for a probe cycle.
    /// </summary>
    public TimeSpan CircuitBreakerResetTimeout { get; init; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Sleep duration between batch cycles when the circuit breaker is open.
    /// Shorter than <see cref="CircuitBreakerResetTimeout"/> so the relay can detect recovery promptly.
    /// </summary>
    public TimeSpan CircuitBreakerCooldownInterval { get; init; } = TimeSpan.FromSeconds(15);

    /// <summary>
    /// How often (in batch cycles) the relay queries for observable gauge metrics
    /// (queue depth, oldest pending age, DLQ size). Set higher to reduce read overhead.
    /// </summary>
    [Range(1, 1000)]
    public int GaugeUpdateCycleInterval { get; init; } = 10;
}
