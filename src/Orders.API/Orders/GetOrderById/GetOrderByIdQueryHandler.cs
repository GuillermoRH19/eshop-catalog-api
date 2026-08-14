using Orders.API.Data;
using Orders.API.Models;

namespace Orders.API.Orders.GetOrderById
{
    public record GetOrderByIdQuery(Guid Id) : IQuery<GetOrderByIdResult>;

    public record GetOrderByIdResult(Order Order);

    public class GetOrderByIdQueryHandler(IOrderRepository orderRepository) : IQueryHandler<GetOrderByIdQuery, GetOrderByIdResult>
    {
        public async Task<GetOrderByIdResult> Handle(GetOrderByIdQuery query, CancellationToken cancellationToken)
        {
            var order = await orderRepository.GetByIdAsync(query.Id, cancellationToken)
                ?? throw new OrderNotFoundException(query.Id);

            return new GetOrderByIdResult(order);
        }
    }
}
