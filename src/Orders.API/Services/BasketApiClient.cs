using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BuildingBlocks.Exceptions;

namespace Orders.API.Services
{
    // Cliente HTTP hacia Basket.API. Se registra vía AddHttpClient<IBasketApiClient, BasketApiClient>
    // en Program.cs, con BaseAddress = Services:BasketApi.
    public class BasketApiClient(HttpClient httpClient, ILogger<BasketApiClient> logger) : IBasketApiClient
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        public async Task<BasketDto?> GetBasketAsync(string userName, CancellationToken cancellationToken = default)
        {
            try
            {
                using var response = await httpClient.GetAsync($"/basket/{Uri.EscapeDataString(userName)}", cancellationToken);

                if (response.StatusCode == HttpStatusCode.NotFound)
                    return null;

                response.EnsureSuccessStatusCode();

                var payload = await response.Content.ReadFromJsonAsync<GetBasketApiResponse>(JsonOptions, cancellationToken);
                return payload?.Cart;
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                logger.LogError(ex, "No se pudo contactar a Basket.API para obtener el carrito de {UserName}", userName);
                throw new InternalServerException("No se pudo contactar al servicio de carrito (Basket.API).");
            }
        }
    }
}
