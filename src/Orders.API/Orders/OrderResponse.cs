namespace Orders.API.Orders
{
    // Respuesta HTTP compartida por los 4 endpoints de Orders. Se proyecta desde Order vía Mapster.
    public record OrderItemResponse(Guid ProductId, string ProductName, int Quantity, decimal UnitPrice, decimal LineTotal);

    public record OrderResponse(
        Guid Id,
        string CustomerId,
        DateTime CreatedAt,
        string Status,
        List<OrderItemResponse> Items,
        decimal Subtotal,
        decimal Tax,
        decimal Total);
}
