namespace Orders.API.Data
{
    // La cadena de conexión NUNCA vive en appsettings.json: se resuelve exclusivamente desde
    // la variable de entorno ConnectionStrings__MongoDb (ver Program.cs). DatabaseName y
    // OrdersCollectionName sí son configuración "normal", no secreta.
    public class MongoDbSettings
    {
        public string ConnectionString { get; set; } = default!;
        public string DatabaseName { get; set; } = "OrdersDb";
        public string OrdersCollectionName { get; set; } = "orders";
    }
}
