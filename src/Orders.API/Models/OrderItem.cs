using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Orders.API.Models
{
    public class OrderItem
    {
        // MongoDB.Driver 3.x exige indicar la representación del Guid explícitamente (antes lo
        // asumía); sin esto, InsertOneAsync lanza BsonSerializationException en cualquier orden
        // con items. Mismo tratamiento que Order.Id.
        [BsonRepresentation(BsonType.String)]
        public Guid ProductId { get; set; }

        public string ProductName { get; set; } = default!;

        public int Quantity { get; set; }

        // Precio unitario capturado al momento de la compra (no se vuelve a consultar Catalog).
        public decimal UnitPrice { get; set; }

        public decimal LineTotal => Math.Round(UnitPrice * Quantity, 2);
    }
}
