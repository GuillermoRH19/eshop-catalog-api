using Orders.API.Services;

namespace Orders.API.Orders.GetOrderById
{
    public class GetOrderPdfEndpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapGet("/api/orders/{id:guid}/pdf", async (
                Guid id,
                ISender sender,
                IOrderPdfGenerator pdfGenerator,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(new GetOrderByIdQuery(id), cancellationToken);
                var bytes = pdfGenerator.Generate(result.Order);
                return Results.File(bytes, "application/pdf", $"orden-{id}.pdf");
            })
            .WithName("GetOrderPdf")
            .Produces(StatusCodes.Status200OK, contentType: "application/pdf")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithSummary("Descargar el comprobante de la orden en PDF")
            .WithDescription("Genera y devuelve el comprobante de compra de la orden en PDF. 404 si la orden no existe.");
        }
    }
}
