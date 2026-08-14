using BuildingBlocks.Exceptions;

namespace Orders.API.Exceptions
{
    // Se lanza cuando el Basket del cliente no existe o no tiene productos: no hay nada con
    // qué generar la orden.
    public class EmptyBasketException : BadRequestException
    {
        public EmptyBasketException(string customerId)
            : base($"El carrito del cliente '{customerId}' está vacío o no existe. No se puede generar una orden.") { }
    }
}
