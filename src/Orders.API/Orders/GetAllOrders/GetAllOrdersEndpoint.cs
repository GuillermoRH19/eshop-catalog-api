namespace Orders.API.Orders.GetAllOrders
{
    public record GetAllOrdersResponse(List<OrderResponse> Orders);

    public class GetAllOrdersEndpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapGet("/api/orders", async (ISender sender, CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(new GetAllOrdersQuery(), cancellationToken);
                var response = new GetAllOrdersResponse(result.Orders.Select(o => o.ToResponse()).ToList());
                return Results.Ok(response);
            })
            .WithName("GetAllOrders")
            .Produces<GetAllOrdersResponse>(StatusCodes.Status200OK)
            .WithSummary("Listar todas las órdenes (vista admin)")
            .WithDescription("Devuelve las 500 órdenes más recientes de todos los clientes, sin filtrar por customerId.");
        }
    }
}
