namespace Orders.API.Models
{
    public class OrderItem
    {
        public Guid ProductId { get; set; }

        public string ProductName { get; set; } = default!;

        public int Quantity { get; set; }

        // Precio unitario capturado al momento de la compra (no se vuelve a consultar Catalog).
        public decimal UnitPrice { get; set; }

        public decimal LineTotal => Math.Round(UnitPrice * Quantity, 2);
    }
}
