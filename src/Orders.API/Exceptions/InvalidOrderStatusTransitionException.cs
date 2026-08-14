using BuildingBlocks.Exceptions;
using Orders.API.Models;

namespace Orders.API.Exceptions
{
    // Transiciones permitidas: Pending -> Confirmed, Pending -> Cancelled.
    // Cualquier otra combinación (incluida Cancelled -> Confirmed) es inválida.
    public class InvalidOrderStatusTransitionException : BadRequestException
    {
        public InvalidOrderStatusTransitionException(OrderStatus from, OrderStatus to)
            : base($"No se permite la transición de estado '{from}' -> '{to}'.") { }
    }
}
