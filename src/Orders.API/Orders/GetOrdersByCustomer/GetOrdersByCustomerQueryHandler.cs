using Orders.API.Data;
using Orders.API.Models;

namespace Orders.API.Orders.GetOrdersByCustomer
{
    public record GetOrdersByCustomerQuery(string CustomerId) : IQuery<GetOrdersByCustomerResult>;

    public record GetOrdersByCustomerResult(IReadOnlyList<Order> Orders);

    public class GetOrdersByCustomerQueryHandler(IOrderRepository orderRepository)
        : IQueryHandler<GetOrdersByCustomerQuery, GetOrdersByCustomerResult>
    {
        public async Task<GetOrdersByCustomerResult> Handle(GetOrdersByCustomerQuery query, CancellationToken cancellationToken)
        {
            var orders = await orderRepository.GetByCustomerIdAsync(query.CustomerId, cancellationToken);
            return new GetOrdersByCustomerResult(orders);
        }
    }
}
