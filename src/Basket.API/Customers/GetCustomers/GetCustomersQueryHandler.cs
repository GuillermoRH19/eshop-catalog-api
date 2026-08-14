using Basket.API.Data;
using Basket.API.Models;

namespace Basket.API.Customers.GetCustomers
{
    public record GetCustomersQuery : IQuery<GetCustomersResult>;

    public record GetCustomersResult(IReadOnlyList<Customer> Customers);

    public class GetCustomersQueryHandler(ICustomerRepository repository) : IQueryHandler<GetCustomersQuery, GetCustomersResult>
    {
        public async Task<GetCustomersResult> Handle(GetCustomersQuery query, CancellationToken cancellationToken)
        {
            var customers = await repository.GetAllAsync(cancellationToken);
            return new GetCustomersResult(customers);
        }
    }
}
