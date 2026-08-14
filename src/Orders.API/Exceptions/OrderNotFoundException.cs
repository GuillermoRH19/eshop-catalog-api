using BuildingBlocks.Exceptions;

namespace Orders.API.Exceptions
{
    public class OrderNotFoundException : NotFoundException
    {
        public OrderNotFoundException(Guid id) : base("Order", id) { }
    }
}
