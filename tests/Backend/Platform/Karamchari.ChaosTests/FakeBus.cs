using MassTransit;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Karamchari.ChaosTests;

/// <summary>
/// Factory for an <see cref="IBus"/> NSubstitute mock whose publish behaviour
/// can be toggled between "online" (records the call) and "offline" (throws to
/// simulate broker unavailability).
///
/// All chaos test assertions are made against <see cref="PublishCount"/> and the
/// underlying NSubstitute mock's received-call tracking.
/// </summary>
public sealed class FakeBus
{
    private readonly IBus _mock;
    private int _publishCount;
    private bool _brokerDown;

    /// <summary>Initializes a new instance of <see cref="FakeBus"/>.</summary>
    public FakeBus()
    {
        _mock = Substitute.For<IBus>();
        ConfigureOnline();
    }

    /// <summary>Gets the underlying <see cref="IBus"/> mock for DI registration.</summary>
    public IBus Mock => _mock;

    /// <summary>Total successful publish calls since construction or last reset.</summary>
    public int PublishCount => _publishCount;

    /// <summary>
    /// Gets or sets whether the broker is simulated as unavailable.
    /// Flipping to <c>true</c> reconfigures the mock to throw on all
    /// <c>Publish</c> overloads; flipping back reconfigures to succeed.
    /// </summary>
    public bool BrokerDown
    {
        get => _brokerDown;
        set
        {
            _brokerDown = value;
            if (value)
                ConfigureOffline();
            else
                ConfigureOnline();
        }
    }

    // Configure the mock so that any Publish call succeeds and increments the counter.
    private void ConfigureOnline()
    {
        _mock.Publish(
                Arg.Any<object>(),
                Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                Interlocked.Increment(ref _publishCount);
                return Task.CompletedTask;
            });

        _mock.Publish(
                Arg.Any<object>(),
                Arg.Any<Type>(),
                Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                Interlocked.Increment(ref _publishCount);
                return Task.CompletedTask;
            });
    }

    // Configure the mock so that any Publish call returns a faulted Task (await throws).
    // NSubstitute .Throws() on Task-returning methods does not reliably override a prior
    // .Returns() setup. Using Task.FromException ensures the faulted task is returned and
    // the exception surfaces when the caller awaits.
    private void ConfigureOffline()
    {
        _mock.Publish(
                Arg.Any<object>(),
                Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromException(
                new InvalidOperationException("Broker is unavailable (simulated fault injection).")));

        _mock.Publish(
                Arg.Any<object>(),
                Arg.Any<Type>(),
                Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromException(
                new InvalidOperationException("Broker is unavailable (simulated fault injection).")));
    }
}
