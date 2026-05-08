using System.ComponentModel.DataAnnotations;

namespace Karamchari.Core.Messaging.Outbox;

/// <summary>
/// Configuration for the production outbox relay service that drains
/// dbo.OutboxMessage to Azure Service Bus.
/// </summary>
public sealed class OutboxRelayOptions
{
    /// <summary>IConfiguration section key.</summary>
    public const string SectionName = "OutboxRelay";

    /// <summary>Messages claimed per polling cycle. 50–200 is typical for ASB throughput.</summary>
    [Range(1, 1000)]
    public int BatchSize { get; init; } = 100;

    /// <summary>How long the relay sleeps between cycles when nothing is pending.</summary>
    public TimeSpan PollingInterval { get; init; } = TimeSpan.FromSeconds(5);

    /// <summary>Max publish attempts before a message is moved to dbo.OutboxDeadLetter.</summary>
    [Range(1, 20)]
    public int MaxRetries { get; init; } = 5;

    /// <summary>Max parallel message-publishes within one batch.</summary>
    [Range(1, 32)]
    public int MaxConcurrency { get; init; } = 4;

    /// <summary>Base delay for exponential backoff on transient publish failures.</summary>
    public TimeSpan InitialRetryDelay { get; init; } = TimeSpan.FromSeconds(2);

    /// <summary>
    /// OutboxState rows with a non-null LockId that are older than this threshold
    /// are assumed to be from a crashed relay instance and are released.
    /// Must be significantly longer than one relay cycle to avoid self-interference.
    /// </summary>
    public TimeSpan StaleLockTimeout { get; init; } = TimeSpan.FromMinutes(10);
}
