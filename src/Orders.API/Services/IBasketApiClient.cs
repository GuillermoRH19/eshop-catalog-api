namespace Orders.API.Services
{
    public interface IBasketApiClient
    {
        // Devuelve null si el carrito no existe (404 en Basket.API) — no es un error, el
        // caller decide qué hacer (típicamente: EmptyBasketException).
        Task<BasketDto?> GetBasketAsync(string userName, CancellationToken cancellationToken = default);
    }
}
