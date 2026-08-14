namespace Orders.API.Services
{
    // DTOs propios de Orders.API para el contrato HTTP de Basket.API (GET /basket/{userName}).
    // No se referencia el proyecto Basket.API: cada microservicio es dueño de su propio contrato.
    public record BasketItemDto(Guid ProductId, string ProductName, string Color, int Quantity, decimal Price);

    public record BasketDto(string UserName, List<BasketItemDto> Items);

    internal record GetBasketApiResponse(BasketDto Cart);
}
