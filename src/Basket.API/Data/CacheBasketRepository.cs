using Basket.API.Models;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using System.Threading;

namespace Basket.API.Data
{
    public class CacheBasketRepository(IBasketRepository repository, IDistributedCache cache) : IBasketRepository
    {
        async Task<bool> IBasketRepository.DeleteBasket(string userName, CancellationToken cancellationToken = default)
        {
            await repository.DeleteBasket(userName, cancellationToken);
            await cache.RemoveAsync(userName, cancellationToken);
            return true;
        }

        async Task<ShoppingCart> IBasketRepository.GetBasket(string userName, CancellationToken cancellationToken = default)
        {
            var cachedBasket = await cache.GetStringAsync(userName, cancellationToken);
            if (!string.IsNullOrEmpty(cachedBasket))
                return JsonSerializer.Deserialize<ShoppingCart>(cachedBasket)!;
            var basket = await repository.GetBasket(userName, cancellationToken);
            return basket;
        }

        async Task<ShoppingCart> IBasketRepository.StoreBasket(ShoppingCart basket, CancellationToken cancellationToken = default)
        {
            await repository.StoreBasket(basket, cancellationToken);
            await cache.SetStringAsync(basket.UserName, JsonSerializer.Serialize(basket), cancellationToken);
            return basket;
        }
    }
}
