using BuildingBlocks.CQRS;
using Marten;
using MediatR;

namespace Catalog.API.Models.Products.CreateProduct
{
    public record CreateProductCommand(string Name, string Description, List<string> Category, string ImageFiles, decimal Price) : ICommand<CreateProductResult>;

    public record CreateProductResult(Guid Id);

    internal class CreateProductCommandHandler(IDocumentSession documentSession) :
    ICommandHandler<CreateProductCommand, CreateProductResult>
    {
        public async Task<CreateProductResult> Handle(CreateProductCommand request, CancellationToken cancellationToken)
        {
            // 1. Creamos la entidad producto
            Product product = new Product
            {
                Name = request.Name,
                Description = request.Description,
                Category = request.Category,
                ImageFiles = request.ImageFiles,
                Price = request.Price
            };

            // 2. Lo preparamos en la sesión de Marten
            documentSession.Store(product);

            // 3. ⚠️ CORREGIDO: 'cancellationToken' en minúscula para usar la variable del método
            await documentSession.SaveChangesAsync(cancellationToken);

            // Nota: Es buena práctica devolver el ID real del producto que se acaba de crear (product.Id) 
            // en lugar de un Guid.NewGuid() aleatorio, para que el cliente sepa qué ID se asignó.
            return new CreateProductResult(product.Id);
        }
    }
}