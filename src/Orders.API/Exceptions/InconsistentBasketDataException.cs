using BuildingBlocks.Exceptions;

namespace Orders.API.Exceptions
{
    // Se lanza cuando algún item del Basket obtenido tiene datos que no se pueden usar
    // para armar una orden (cantidad <= 0, precio negativo, nombre vacío, etc).
    public class InconsistentBasketDataException : BadRequestException
    {
        public InconsistentBasketDataException(string message) : base(message) { }
    }
}
