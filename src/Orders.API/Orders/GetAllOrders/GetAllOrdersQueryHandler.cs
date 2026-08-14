using Orders.API.Data;
using Orders.API.Models;

namespace Orders.API.Orders.GetAllOrders
{
    public record GetAllOrdersQuery : IQuery<GetAllOrdersResult>;

    public record GetAllOrdersResult(IReadOnlyList<Order> Orders);

    // Sin login real: "admin" es simplemente el nombre de usuario que el frontend trata como
    // administrador para pedir este endpoint en vez de GET /api/orders/customer/{id}.
    public class GetAllOrdersQueryHandler(IOrderRepository orderRepository) : IQueryHandler<GetAllOrdersQuery, GetAllOrdersResult>
    {
        public async Task<GetAllOrdersResult> Handle(GetAllOrdersQuery query, CancellationToken cancellationToken)
        {
            var orders = await orderRepository.GetAllAsync(cancellationToken);
            return new GetAllOrdersResult(orders);
        }
    }
}
