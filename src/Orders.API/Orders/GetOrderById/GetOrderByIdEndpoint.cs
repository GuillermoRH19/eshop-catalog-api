namespace Orders.API.Orders.GetOrderById
{
    public class GetOrderByIdEndpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapGet("/api/orders/{id:guid}", async (Guid id, ISender sender, CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(new GetOrderByIdQuery(id), cancellationToken);
                return Results.Ok(result.Order.ToResponse());
            })
            .WithName("GetOrderById")
            .Produces<OrderResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithSummary("Consultar una orden por Id")
            .WithDescription("Devuelve la orden con el Id indicado, o 404 si no existe.");
        }
    }
}
