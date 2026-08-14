namespace Orders.API.Orders.CreateOrder
{
    public record CreateOrderRequest(string CustomerId);

    public class CreateOrderEndpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPost("/api/orders", async (
                CreateOrderRequest request,
                HttpRequest httpRequest,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var idempotencyKey = httpRequest.Headers["Idempotency-Key"].ToString();
                var command = new CreateOrderCommand(request.CustomerId, idempotencyKey);
                var result = await sender.Send(command, cancellationToken);
                var response = result.Order.ToResponse();

                // Primera vez: 201 Created. Reintento con el mismo Idempotency-Key: 200 OK con la
                // misma orden (no se duplicó nada).
                return result.AlreadyExisted
                    ? Results.Ok(response)
                    : Results.Created($"/api/orders/{response.Id}", response);
            })
            .WithName("CreateOrder")
            .Produces<OrderResponse>(StatusCodes.Status201Created)
            .Produces<OrderResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .WithSummary("Crear una orden a partir del Basket del cliente")
            .WithDescription(
                "Consulta el Basket de CustomerId en Basket.API, valida que tenga productos y datos " +
                "consistentes, calcula subtotal/impuestos/total y guarda la orden en MongoDB Atlas. " +
                "Requiere el header 'Idempotency-Key'; si se repite, devuelve la orden ya creada (200) " +
                "en vez de duplicarla.");
        }
    }
}
