using System.Security.Cryptography;

namespace Orders.API.Orders
{
    // El Id real de una orden/producto sigue siendo el Guid completo (es la clave real en Mongo/
    // Postgres, no se toca) — esto solo deriva un código corto y legible para mostrar en el
    // ticket/PDF y en la UI. Determinístico: el mismo Guid siempre da el mismo código corto, no
    // hay estado ni contador que mantener.
    public static class OrderIdFormatter
    {
        public static string ToOrderNumber(this Guid id) => $"ORD-{id.ToShortCode()}";

        // Se usa un hash del Guid, NO una porción cruda de él: los Id de Catalog/Basket (Marten)
        // no son aleatorios uniformes, son tipo "sequential" (ordenados por tiempo de creación) —
        // productos creados en la misma ráfaga comparten los primeros caracteres del Guid casi
        // completos. Tomar solo esos primeros caracteres producía códigos repetidos entre
        // productos distintos. El hash distribuye bien sin importar cómo se generó el Guid.
        public static string ToShortCode(this Guid id)
        {
            var hash = SHA256.HashData(id.ToByteArray());
            return Convert.ToHexString(hash, 0, 4);
        }
    }
}
