namespace AgentShop.Api.IntegrationTests.Infrastructure;

/// <summary>
/// Base class for HTTP tests: resets the database before every test and exposes an HttpClient.
/// </summary>
public abstract class IntegrationTest(AgentShopApiFactory factory) : IAsyncLifetime
{
    protected AgentShopApiFactory Factory { get; } = factory;

    protected HttpClient Client { get; } = factory.CreateClient();

    protected static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync() => await Factory.ResetAsync();

    public ValueTask DisposeAsync()
    {
        Client.Dispose();
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }
}
