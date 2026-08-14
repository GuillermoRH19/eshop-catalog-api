using Basket.API.Models;

namespace Basket.API.Data
{
    public class CustomerRepository(IDocumentSession session) : ICustomerRepository
    {
        public async Task<(Customer Customer, bool IsNew)> GetOrCreateAsync(string name, CancellationToken cancellationToken = default)
        {
            var existing = await session.LoadAsync<Customer>(name, cancellationToken);
            if (existing is not null)
                return (existing, false);

            var customer = new Customer { Name = name, CreatedAt = DateTime.UtcNow };
            session.Store(customer);
            await session.SaveChangesAsync(cancellationToken);
            return (customer, true);
        }
    }
}
