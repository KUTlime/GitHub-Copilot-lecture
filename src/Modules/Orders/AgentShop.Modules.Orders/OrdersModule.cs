using AgentShop.Modules.Orders.Features.MarkOrderShipped;
using AgentShop.Modules.Orders.Persistence;
using AgentShop.Modules.Shipping.Contracts;
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

namespace AgentShop.Modules.Orders;

/// <summary>
/// The only public type of the Orders module: registration, endpoint mapping and migrations.
/// </summary>
public static class OrdersModule
{
    private const string ConnectionName = "agentshop";

    public static IHostApplicationBuilder AddOrdersModule(this IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddDbContext<OrdersDbContext>((services, options) =>
            options.UseNpgsql(
                services.GetRequiredService<IConfiguration>().GetConnectionString(ConnectionName),
                npgsql => npgsql.MigrationsHistoryTable(OrdersDbContext.MigrationsHistoryTable, OrdersDbContext.Schema)));
        builder.EnrichNpgsqlDbContext<OrdersDbContext>();

        builder.Services.AddValidatorsFromAssembly(typeof(OrdersModule).Assembly, includeInternalTypes: true);

        // Integration event handlers are registered explicitly so every subscription is easy to find.
        builder.Services.AddScoped<IIntegrationEventHandler<ShipmentDispatchedIntegrationEvent>, ShipmentDispatchedHandler>();

        return builder;
    }

    public static IEndpointRouteBuilder MapOrdersModule(this IEndpointRouteBuilder app)
    {
        app.MapGroup("/orders")
            .WithTags("Orders")
            .MapEndpoints(typeof(OrdersModule).Assembly);

        return app;
    }

    public static async Task MigrateOrdersModuleAsync(this IServiceProvider services, CancellationToken cancellationToken)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
        await db.Database.MigrateAsync(cancellationToken);
    }
}
