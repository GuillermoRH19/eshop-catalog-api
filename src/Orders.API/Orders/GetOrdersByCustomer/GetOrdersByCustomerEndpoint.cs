namespace Orders.API.Orders.GetOrdersByCustomer
{
    public record GetOrdersByCustomerResponse(List<OrderResponse> Orders);

    public class GetOrdersByCustomerEndpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapGet("/api/orders/customer/{customerId}", async (string customerId, ISender sender, CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(new GetOrdersByCustomerQuery(customerId), cancellationToken);
                var response = new GetOrdersByCustomerResponse(result.Orders.Select(o => o.ToResponse()).ToList());
                return Results.Ok(response);
            })
            .WithName("GetOrdersByCustomer")
            .Produces<GetOrdersByCustomerResponse>(StatusCodes.Status200OK)
            .WithSummary("Listar las órdenes de un cliente")
            .WithDescription("Devuelve todas las órdenes de customerId, más recientes primero. Lista vacía si no tiene ninguna.");
        }
    }
}
