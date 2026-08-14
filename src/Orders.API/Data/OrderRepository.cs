using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Orders.API.Models;

namespace Orders.API.Data
{
    // IMongoClient se registra como singleton en Program.cs (recomendación oficial del driver y
    // requisito del health check de MongoDB), así que acá solo se resuelve la colección a partir
    // de él en vez de crear una instancia propia.
    public class OrderRepository : IOrderRepository
    {
        private readonly IMongoCollection<Order> _orders;
        private readonly ILogger<OrderRepository> _logger;

        public OrderRepository(IMongoClient client, IOptions<MongoDbSettings> options, ILogger<OrderRepository> logger)
        {
            _logger = logger;
            var settings = options.Value;
            var database = client.GetDatabase(settings.DatabaseName);
            _orders = database.GetCollection<Order>(settings.OrdersCollectionName);

            // Único índice necesario: IdempotencyKey no se puede repetir. Sparse porque en teoría
            // no debería haber orders sin ella, pero evita romper si alguna vez se inserta una.
            var indexKeys = Builders<Order>.IndexKeys.Ascending(o => o.IdempotencyKey);
            var indexModel = new CreateIndexModel<Order>(indexKeys, new CreateIndexOptions { Unique = true, Sparse = true });
            _orders.Indexes.CreateOne(indexModel);
        }

        public async Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _orders.Find(o => o.Id == id).FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<Order?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default)
        {
            return await _orders.Find(o => o.IdempotencyKey == idempotencyKey).FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Order>> GetByCustomerIdAsync(string customerId, CancellationToken cancellationToken = default)
        {
            return await _orders.Find(o => o.CustomerId == customerId)
                .SortByDescending(o => o.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<bool> TryCreateAsync(Order order, CancellationToken cancellationToken = default)
        {
            try
            {
                await _orders.InsertOneAsync(order, options: null, cancellationToken);
                return true;
            }
            catch (MongoWriteException ex) when (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
            {
                // Reintento con el mismo Idempotency-Key: no es un error, el caller debe
                // recuperar la orden ya existente.
                return false;
            }
            catch (MongoException ex)
            {
                // Loggeamos el detalle real (host, categoría del error) solo del lado del servidor.
                // Al cliente solo le llega un mensaje genérico: el CustomExceptionHandler nunca
                // expone exception.Message de un MongoException.
                _logger.LogError(ex, "Error de MongoDB al crear la orden {OrderId}", order.Id);
                throw new BuildingBlocks.Exceptions.InternalServerException("No se pudo guardar la orden en la base de datos.");
            }
        }

        public async Task UpdateStatusAsync(Guid id, OrderStatus status, CancellationToken cancellationToken = default)
        {
            var update = Builders<Order>.Update.Set(o => o.Status, status);
            try
            {
                await _orders.UpdateOneAsync(o => o.Id == id, update, cancellationToken: cancellationToken);
            }
            catch (MongoException ex)
            {
                _logger.LogError(ex, "Error de MongoDB al actualizar el estado de la orden {OrderId}", id);
                throw new BuildingBlocks.Exceptions.InternalServerException("No se pudo actualizar el estado de la orden en la base de datos.");
            }
        }
    }
}
