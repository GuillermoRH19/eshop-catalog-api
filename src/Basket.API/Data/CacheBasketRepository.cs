using Basket.API.Models;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Threading;

namespace Basket.API.Data
{
    // Redis es solo caché: Postgres (vía IBasketRepository) es la fuente de verdad. Si Redis está
    // caído/inalcanzable (visto en producción: timeouts contra Upstash), antes esto tumbaba TODO
    // Basket.API con un 500 y exponía el stack de RedisConnectionException al cliente, aunque
    // Postgres estuviera perfectamente sano. Ahora cualquier falla de Redis se loggea y se
    // degrada a Postgres directo, en vez de fallar la request completa.
    public class CacheBasketRepository(IBasketRepository repository, IDistributedCache cache, ILogger<CacheBasketRepository> logger)
        : IBasketRepository
    {
        async Task<bool> IBasketRepository.DeleteBasket(string userName, CancellationToken cancellationToken)
        {
            await repository.DeleteBasket(userName, cancellationToken);
            try
            {
                await cache.RemoveAsync(userName, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "No se pudo invalidar el caché de Redis para {UserName}; el borrado en Postgres ya se aplicó.", userName);
            }
            return true;
        }

        async Task<ShoppingCart> IBasketRepository.GetBasket(string userName, CancellationToken cancellationToken)
        {
            try
            {
                var cachedBasket = await cache.GetStringAsync(userName, cancellationToken);
                if (!string.IsNullOrEmpty(cachedBasket))
                    return JsonSerializer.Deserialize<ShoppingCart>(cachedBasket)!;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Redis no disponible al leer el carrito de {UserName}; se consulta Postgres directamente.", userName);
            }

            return await repository.GetBasket(userName, cancellationToken);
        }

        async Task<ShoppingCart> IBasketRepository.StoreBasket(ShoppingCart basket, CancellationToken cancellationToken)
        {
            await repository.StoreBasket(basket, cancellationToken);
            try
            {
                await cache.SetStringAsync(basket.UserName, JsonSerializer.Serialize(basket), cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "No se pudo actualizar el caché de Redis para {UserName}; el guardado en Postgres ya se aplicó.", basket.UserName);
            }
            return basket;
        }
    }
}
