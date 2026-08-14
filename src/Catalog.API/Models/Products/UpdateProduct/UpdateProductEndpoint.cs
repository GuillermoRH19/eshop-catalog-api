using MediatR;

namespace Catalog.API.Models.Products.UpdateProduct
{
    public record UpdateProductRequest(string Name, string Description, List<string> Category, string ImageFiles, decimal Price);

    public record UpdateProductResponse(Guid Id);

    public class UpdateProductEndpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPut("/products/{id}", async (Guid id, UpdateProductRequest request, ISender sender) =>
            {
                var command = new UpdateProductCommand(id, request.Name, request.Description, request.Category, request.ImageFiles, request.Price);

                var result = await sender.Send(command);
                var response = result.Adapt<UpdateProductResponse>();
                return Results.Ok(response);
            })
                .WithName("ActualizarProducto")
                .Produces<UpdateProductResponse>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .WithSummary("Actualizar un producto existente")
                .WithDescription("Actualiza los datos de un producto por su Id");
        }
    }
}
