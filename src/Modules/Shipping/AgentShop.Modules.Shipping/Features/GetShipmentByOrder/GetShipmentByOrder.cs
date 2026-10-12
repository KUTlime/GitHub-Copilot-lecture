using AgentShop.Modules.Shipping.Domain;
using AgentShop.Modules.Shipping.Persistence;
using AgentShop.SharedKernel;
using AgentShop.SharedKernel.AspNetCore;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace AgentShop.Modules.Shipping.Features.GetShipmentByOrder;

internal static class GetShipmentByOrder
{
    public sealed record Request(Guid OrderId);

    public sealed record Response(Guid Id, Guid OrderId, string Status, DateTimeOffset CreatedAt, DateTimeOffset? DispatchedAt);

    internal sealed class Validator : AbstractValidator<Request>
    {
        public Validator() => RuleFor(x => x.OrderId).NotEmpty();
    }

    internal static async Task<Result<Response>> HandleAsync(Request request, ShippingDbContext db, CancellationToken cancellationToken)
    {
        var shipment = await db.Shipments
            .AsNoTracking()
            .SingleOrDefaultAsync(s => s.OrderId == request.OrderId, cancellationToken);

        return shipment is null
            ? ShippingErrors.ShipmentNotFound
            : new Response(shipment.Id.Value, shipment.OrderId, shipment.Status.ToString(), shipment.CreatedAt, shipment.DispatchedAt);
    }

    internal sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app) =>
            app.MapGet("/", async ([AsParameters] Request request, ShippingDbContext db, CancellationToken cancellationToken) =>
                    (await HandleAsync(request, db, cancellationToken)).ToHttpResult())
               .AddEndpointFilter<ValidationFilter<Request>>()
               .WithName(nameof(GetShipmentByOrder))
               .Produces<Response>()
               .ProducesValidationProblem()
               .ProducesProblem(StatusCodes.Status404NotFound);
    }
}
