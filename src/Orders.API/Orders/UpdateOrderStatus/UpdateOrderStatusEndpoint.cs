namespace Orders.API.Orders.UpdateOrderStatus
{
    public record UpdateOrderStatusRequest(string Status);

    public class UpdateOrderStatusEndpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPatch("/api/orders/{id:guid}/status", async (
                Guid id,
                UpdateOrderStatusRequest request,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var command = new UpdateOrderStatusCommand(id, request.Status);
                var result = await sender.Send(command, cancellationToken);
                return Results.Ok(result.Order.ToResponse());
            })
            .WithName("UpdateOrderStatus")
            .Produces<OrderResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithSummary("Cambiar el estado de una orden")
            .WithDescription("Transiciones permitidas: Pending -> Confirmed, Pending -> Cancelled. Cualquier otra combinación devuelve 400.");
        }
    }
}
