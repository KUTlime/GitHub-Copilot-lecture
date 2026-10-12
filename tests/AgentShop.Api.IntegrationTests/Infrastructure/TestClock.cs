namespace AgentShop.Api.IntegrationTests.Infrastructure;

/// <summary>
/// Settable clock for the shared API host. Unlike FakeTimeProvider it can be reset back to the start
/// time before each test, which keeps timestamps in snapshots deterministic.
/// </summary>
public sealed class TestClock(DateTimeOffset start) : TimeProvider
{
    private DateTimeOffset _utcNow = start;

    public override DateTimeOffset GetUtcNow() => _utcNow;

    public void Set(DateTimeOffset utcNow) => _utcNow = utcNow;

    public void Advance(TimeSpan delta) => _utcNow += delta;
}
