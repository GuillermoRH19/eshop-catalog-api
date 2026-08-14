namespace Basket.API.Customers.SwitchUser
{
    public record SwitchUserRequest(string Name);

    public record SwitchUserResponse(string Name, DateTime CreatedAt, bool IsNew);

    // No es login: no hay contraseña ni sesión. Simplemente registra/reconoce un nombre de
    // usuario en la base de datos para que Basket y Orders tengan un identificador único y
    // persistente por cliente (ver "Cambiar usuario" en el frontend).
    public class SwitchUserEndpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPost("/customers", async (SwitchUserRequest request, ISender sender) =>
            {
                var result = await sender.Send(new SwitchUserCommand(request.Name));
                var response = result.Adapt<SwitchUserResponse>();
                return Results.Ok(response);
            })
            .WithName("SwitchUser")
            .Produces<SwitchUserResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .WithSummary("Registrar o reconocer un usuario por nombre")
            .WithDescription("Crea el usuario si no existe (o lo reconoce si ya existía) y lo devuelve. No hay login: el nombre es el identificador único de Basket y Orders.");
        }
    }
}
