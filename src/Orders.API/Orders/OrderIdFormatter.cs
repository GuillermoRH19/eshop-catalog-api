namespace Orders.API.Orders
{
    // El Id real de una orden/producto sigue siendo el Guid completo (es la clave real en Mongo/
    // Postgres, no se toca) — esto solo deriva un código corto y legible para mostrar en el
    // ticket/PDF y en la UI. Determinístico: el mismo Guid siempre da el mismo código corto, no
    // hay estado ni contador que mantener.
    public static class OrderIdFormatter
    {
        public static string ToOrderNumber(this Guid id) => $"ORD-{id.ToShortCode()}";

        public static string ToShortCode(this Guid id) => id.ToString("N")[..8].ToUpperInvariant();
    }
}
