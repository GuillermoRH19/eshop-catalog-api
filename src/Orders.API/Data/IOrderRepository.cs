using Orders.API.Models;

namespace Orders.API.Data
{
    public interface IOrderRepository
    {
        Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        Task<Order?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<Order>> GetByCustomerIdAsync(string customerId, CancellationToken cancellationToken = default);

        // Para el usuario "admin" del frontend (sin login real): ver todas las órdenes de todos
        // los clientes, no solo las propias. Limitado a las 500 más recientes.
        Task<IReadOnlyList<Order>> GetAllAsync(CancellationToken cancellationToken = default);

        // Devuelve false (sin lanzar) cuando ya existe una orden con el mismo Idempotency-Key,
        // para que el caller decida qué hacer (ej. devolver la orden previa) en vez de tratarlo
        // como un error de base de datos.
        Task<bool> TryCreateAsync(Order order, CancellationToken cancellationToken = default);

        Task UpdateStatusAsync(Guid id, OrderStatus status, CancellationToken cancellationToken = default);
    }
}
