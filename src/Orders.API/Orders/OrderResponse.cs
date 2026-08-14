namespace Orders.API.Orders
{
    // Respuesta HTTP compartida por los 4 endpoints de Orders. Se proyecta desde Order vía Mapster.
    // ProductRef/OrderNumber son códigos cortos derivados del Guid real (ver OrderIdFormatter) —
    // el Id/ProductId completos se siguen mandando para trazabilidad, pero para mostrarle algo
    // legible a un humano (recibo, ticket) nadie quiere leer un GUID de 36 caracteres.
    public record OrderItemResponse(Guid ProductId, string ProductRef, string ProductName, int Quantity, decimal UnitPrice, decimal LineTotal);

    public record OrderResponse(
        Guid Id,
        string OrderNumber,
        string CustomerId,
        DateTime CreatedAt,
        string Status,
        List<OrderItemResponse> Items,
        decimal Subtotal,
        decimal Tax,
        decimal Total);
}
