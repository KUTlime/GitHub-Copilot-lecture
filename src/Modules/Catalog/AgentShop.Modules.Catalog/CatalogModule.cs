using AgentShop.Modules.Catalog.Contracts;
using AgentShop.Modules.Catalog.Persistence;
using AgentShop.Modules.Catalog.PublicApi;
using AgentShop.SharedKernel.AspNetCore;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AgentShop.Modules.Catalog;

/// <summary>
/// The only public type of the Catalog module: registration, endpoint mapping and migrations.
/// </summary>
public static class CatalogModule
{
    private const string ConnectionName = "agentshop";

    public static IHostApplicationBuilder AddCatalogModule(this IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddDbContext<CatalogDbContext>((services, options) =>
            options.UseNpgsql(
                services.GetRequiredService<IConfiguration>().GetConnectionString(ConnectionName),
                npgsql => npgsql.MigrationsHistoryTable(CatalogDbContext.MigrationsHistoryTable, CatalogDbContext.Schema)));
        builder.EnrichNpgsqlDbContext<CatalogDbContext>();

        builder.Services.AddValidatorsFromAssembly(typeof(CatalogModule).Assembly, includeInternalTypes: true);
        builder.Services.AddScoped<ICatalogModule, CatalogModuleApi>();

        return builder;
    }

    public static IEndpointRouteBuilder MapCatalogModule(this IEndpointRouteBuilder app)
    {
        app.MapGroup("/catalog/products")
            .WithTags("Catalog")
            .MapEndpoints(typeof(CatalogModule).Assembly);

        return app;
    }

    public static async Task MigrateCatalogModuleAsync(this IServiceProvider services, CancellationToken cancellationToken)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        await db.Database.MigrateAsync(cancellationToken);
    }
}
