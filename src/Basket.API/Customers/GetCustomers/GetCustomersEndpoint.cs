using Basket.API.Models;

namespace Basket.API.Customers.GetCustomers
{
    public record GetCustomersResponse(IEnumerable<Customer> Customers);

    public class GetCustomersEndpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapGet("/customers", async (ISender sender) =>
            {
                var result = await sender.Send(new GetCustomersQuery());
                var response = result.Adapt<GetCustomersResponse>();
                return Results.Ok(response);
            })
            .WithName("GetCustomers")
            .Produces<GetCustomersResponse>(StatusCodes.Status200OK)
            .WithSummary("Listar usuarios registrados")
            .WithDescription("Devuelve todos los nombres de usuario ya registrados, para elegir uno en vez de escribirlo a ciegas al 'Cambiar usuario'.");
        }
    }
}
