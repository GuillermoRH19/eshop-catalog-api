namespace Basket.API.Models
{
    // "Usuario" liviano sin login: el frontend deja escribir cualquier nombre desde la barra de
    // navegación ("Cambiar usuario"). Name es el identificador único (igual que ShoppingCart.UserName
    // y Order.CustomerId) — no hay un Id separado porque todo el sistema ya identifica al cliente
    // por ese nombre.
    public class Customer
    {
        public string Name { get; set; } = default!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
