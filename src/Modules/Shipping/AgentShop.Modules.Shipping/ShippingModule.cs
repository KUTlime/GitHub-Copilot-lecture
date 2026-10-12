using AgentShop.Modules.Orders.Contracts;
using AgentShop.Modules.Shipping.Features.CreateShipment;
using AgentShop.Modules.Shipping.Persistence;
using AgentShop.SharedKernel;
using AgentShop.SharedKernel.AspNetCore;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AgentShop.Modules.Shipping;

/// <summary>
/// The only public type of the Shipping module: registration, endpoint mapping and migrations.
/// </summary>
public static class ShippingModule
{
    private const string ConnectionName = "agentshop";

    public static IHostApplicationBuilder AddShippingModule(this IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddDbContext<ShippingDbContext>((services, options) =>
            options.UseNpgsql(
                services.GetRequiredService<IConfiguration>().GetConnectionString(ConnectionName),
                npgsql => npgsql.MigrationsHistoryTable(ShippingDbContext.MigrationsHistoryTable, ShippingDbContext.Schema)));
        builder.EnrichNpgsqlDbContext<ShippingDbContext>();

        builder.Services.AddValidatorsFromAssembly(typeof(ShippingModule).Assembly, includeInternalTypes: true);

        // Integration event handlers are registered explicitly so every subscription is easy to find.
        builder.Services.AddScoped<IIntegrationEventHandler<OrderPlacedIntegrationEvent>, OrderPlacedHandler>();

        return builder;
    }

    public static IEndpointRouteBuilder MapShippingModule(this IEndpointRouteBuilder app)
    {
        app.MapGroup("/shipping/shipments")
            .WithTags("Shipping")
            .MapEndpoints(typeof(ShippingModule).Assembly);

        return app;
    }

    public static async Task MigrateShippingModuleAsync(this IServiceProvider services, CancellationToken cancellationToken)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ShippingDbContext>();
        await db.Database.MigrateAsync(cancellationToken);
    }
}
