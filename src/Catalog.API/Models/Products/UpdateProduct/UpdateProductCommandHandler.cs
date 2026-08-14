using BuildingBlocks.CQRS;
using Catalog.API.Exceptions;
using Marten;
using MediatR;

namespace Catalog.API.Models.Products.UpdateProduct
{
    public record UpdateProductCommand(Guid Id, string Name, string Description, List<string> Category, string ImageFiles, decimal Price)
        : ICommand<UpdateProductResult>;

    public record UpdateProductResult(Guid Id);

    internal class UpdateProductCommandHandler(IDocumentSession documentSession) :
        ICommandHandler<UpdateProductCommand, UpdateProductResult>
    {
        public async Task<UpdateProductResult> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
        {
            var product = await documentSession.LoadAsync<Product>(request.Id, cancellationToken)
                ?? throw new ProductNotFoundException();

            product.Name = request.Name;
            product.Description = request.Description;
            product.Category = request.Category;
            product.ImageFiles = request.ImageFiles;
            product.Price = request.Price;

            documentSession.Update(product);
            await documentSession.SaveChangesAsync(cancellationToken);

            return new UpdateProductResult(product.Id);
        }
    }
}
