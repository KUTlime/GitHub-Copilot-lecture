using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using Respawn;
using Respawn.Graph;
using Testcontainers.PostgreSql;

namespace AgentShop.Api.IntegrationTests.Infrastructure;

/// <summary>
/// Hosts the real API (Program.cs) against a PostgreSQL container. Migrations run on startup
/// (Development environment). Time is controlled by <see cref="TestClock"/> for deterministic snapshots.
/// </summary>
public sealed class AgentShopApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public static readonly DateTimeOffset StartTime = new(2026, 1, 15, 10, 0, 0, TimeSpan.Zero);

    private static readonly string[] Schemas = ["catalog", "orders", "shipping"];

    // Same major/minor version as the Aspire PostgreSQL integration uses at runtime.
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("docker.io/library/postgres:18.3").Build();

    private Respawner? _respawner;

    public TestClock Clock { get; } = new(StartTime);

    public async ValueTask InitializeAsync()
    {
        await _postgres.StartAsync(TestContext.Current.CancellationToken);

        // Building the host runs Program.cs, which applies all module migrations.
        _ = Services;

        await using var connection = await OpenConnectionAsync();
        _respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = Schemas,
            // Keep the migrations history, otherwise EF Core would think the schema is not migrated.
            TablesToIgnore = [.. Schemas.Select(schema => new Table(schema, "__EFMigrationsHistory"))],
        });
    }

    public async Task ResetAsync()
    {
        await using var connection = await OpenConnectionAsync();
        await _respawner!.ResetAsync(connection);
        Clock.Set(StartTime);
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:agentshop", _postgres.GetConnectionString());
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Clock);
        });
    }

    private async Task<NpgsqlConnection> OpenConnectionAsync()
    {
        var connection = new NpgsqlConnection(_postgres.GetConnectionString());
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        return connection;
    }
}
