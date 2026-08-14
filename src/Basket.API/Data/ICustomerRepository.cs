using Basket.API.Models;

namespace Basket.API.Data
{
    public interface ICustomerRepository
    {
        // Si el nombre ya existe, lo devuelve tal cual (IsNew=false). Si no, lo crea (IsNew=true).
        // No lanza NotFound: "cambiar de usuario" a un nombre nuevo es un flujo válido, no un error.
        Task<(Customer Customer, bool IsNew)> GetOrCreateAsync(string name, CancellationToken cancellationToken = default);

        // Para el modal "Cambiar usuario": lista de usuarios ya registrados para elegir en vez
        // de escribir a ciegas.
        Task<IReadOnlyList<Customer>> GetAllAsync(CancellationToken cancellationToken = default);
    }
}
