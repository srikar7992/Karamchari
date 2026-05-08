using Microsoft.Extensions.Options;

namespace Karamchari.Core.Messaging.Outbox;

/// <summary>
/// Thread-safe three-state circuit breaker for the outbox relay.
/// Prevents hammering Azure Service Bus and the SQL database during sustained outages.
///
/// State machine:
/// <list type="bullet">
///   <item><c>Closed</c>  — normal operation; failures counted toward threshold.</item>
///   <item><c>Open</c>    — relay skips batch cycles; waits for reset timeout to elapse.</item>
///   <item><c>HalfOpen</c>— one probe batch allowed through; success → Closed, failure → Open.</item>
/// </list>
/// </summary>
public sealed class OutboxRelayCircuitBreaker
{
    private enum CircuitState { Closed, Open, HalfOpen }

    private CircuitState _state = CircuitState.Closed;
    private int _consecutiveFailures;
    private DateTimeOffset _openUntil = DateTimeOffset.MinValue;
    private readonly int _failureThreshold;
    private readonly TimeSpan _resetTimeout;
    private readonly object _syncRoot = new();

    /// <summary>Initializes the circuit breaker from relay options.</summary>
    public OutboxRelayCircuitBreaker(IOptions<OutboxRelayOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _failureThreshold = options.Value.CircuitBreakerFailureThreshold;
        _resetTimeout = options.Value.CircuitBreakerResetTimeout;
    }

    /// <summary>
    /// Returns <c>true</c> when the circuit is Open and the reset window has not elapsed.
    /// Automatically transitions <c>Open → HalfOpen</c> once the reset window expires.
    /// When <c>true</c>, the relay should skip the batch cycle entirely.
    /// </summary>
    public bool ShouldSkip
    {
        get
        {
            lock (_syncRoot)
            {
                if (_state == CircuitState.Open && DateTimeOffset.UtcNow >= _openUntil)
                    _state = CircuitState.HalfOpen;

                return _state == CircuitState.Open;
            }
        }
    }

    /// <summary>Current state label for metrics export.</summary>
    public string StateLabel
    {
        get { lock (_syncRoot) { return _state.ToString(); } }
    }

    /// <summary>Current consecutive failure count for diagnostics.</summary>
    public int ConsecutiveFailures
    {
        get { lock (_syncRoot) { return _consecutiveFailures; } }
    }

    /// <summary>
    /// Records a successful publish. Resets failure count and closes the circuit.
    /// </summary>
    public void RecordSuccess()
    {
        lock (_syncRoot)
        {
            _consecutiveFailures = 0;
            _state = CircuitState.Closed;
        }
    }

    /// <summary>
    /// Records a failed publish. Increments consecutive failure count.
    /// Transitions <c>Closed → Open</c> when threshold reached.
    /// Transitions <c>HalfOpen → Open</c> immediately on any failure.
    /// </summary>
    public void RecordFailure()
    {
        lock (_syncRoot)
        {
            _consecutiveFailures++;
            if (_consecutiveFailures >= _failureThreshold || _state == CircuitState.HalfOpen)
            {
                _state = CircuitState.Open;
                _openUntil = DateTimeOffset.UtcNow + _resetTimeout;
                _consecutiveFailures = 0;
            }
        }
    }
}
