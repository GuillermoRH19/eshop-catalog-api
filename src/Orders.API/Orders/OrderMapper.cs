using Orders.API.Models;

namespace Orders.API.Orders
{
    // Mapster (usado en el resto del proyecto) resuelve bien los records "pass-through" que ya
    // existen en Basket/Catalog, pero acá el mapeo no es 1:1 (enum -> string, item -> item con
    // LineTotal calculado), así que se hace explícito para no depender de su inferencia automática.
    public static class OrderMapper
    {
        public static OrderResponse ToResponse(this Order order) => new(
            order.Id,
            order.CustomerId,
            order.CreatedAt,
            order.Status.ToString(),
            order.Items.Select(i => new OrderItemResponse(i.ProductId, i.ProductName, i.Quantity, i.UnitPrice, i.LineTotal)).ToList(),
            order.Subtotal,
            order.Tax,
            order.Total);
    }
}
