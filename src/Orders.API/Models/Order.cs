using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Orders.API.Models
{
    public class Order
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public Guid Id { get; set; } = Guid.NewGuid();

        // Identifica al dueño del carrito/orden. Coincide con ShoppingCart.UserName en Basket.API.
        public string CustomerId { get; set; } = default!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [BsonRepresentation(BsonType.String)]
        public OrderStatus Status { get; set; } = OrderStatus.Pending;

        public List<OrderItem> Items { get; set; } = new();

        public decimal Subtotal { get; set; }

        public decimal Tax { get; set; }

        public decimal Total { get; set; }

        // Header Idempotency-Key del request que creó la orden. Único en la colección
        // para poder detectar reintentos y devolver la orden ya creada sin duplicarla.
        public string IdempotencyKey { get; set; } = default!;
    }
}
