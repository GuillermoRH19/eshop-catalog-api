using FluentValidation;
using Microsoft.Extensions.Configuration;
using Orders.API.Data;
using Orders.API.Models;
using Orders.API.Services;

namespace Orders.API.Orders.CreateOrder
{
    public record CreateOrderCommand(string CustomerId, string IdempotencyKey) : ICommand<CreateOrderResult>;

    // AlreadyExisted=true cuando el Idempotency-Key ya se había usado: la orden devuelta es la
    // original, no se creó ni se duplicó nada.
    public record CreateOrderResult(Order Order, bool AlreadyExisted);

    public class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
    {
        public CreateOrderCommandValidator()
        {
            RuleFor(x => x.CustomerId).NotEmpty().WithMessage("CustomerId es requerido.");
            RuleFor(x => x.IdempotencyKey).NotEmpty().WithMessage("El header 'Idempotency-Key' es requerido.");
        }
    }

    public class CreateOrderCommandHandler(
        IOrderRepository orderRepository,
        IBasketApiClient basketApiClient,
        IConfiguration configuration)
        : ICommandHandler<CreateOrderCommand, CreateOrderResult>
    {
        public async Task<CreateOrderResult> Handle(CreateOrderCommand command, CancellationToken cancellationToken)
        {
            // 1. ¿Ya se procesó este Idempotency-Key? Si sí, devolvemos la orden original tal cual.
            var existingOrder = await orderRepository.GetByIdempotencyKeyAsync(command.IdempotencyKey, cancellationToken);
            if (existingOrder is not null)
                return new CreateOrderResult(existingOrder, AlreadyExisted: true);

            // 2. Obtener el Basket del cliente.
            var basket = await basketApiClient.GetBasketAsync(command.CustomerId, cancellationToken);
            if (basket is null || basket.Items.Count == 0)
                throw new EmptyBasketException(command.CustomerId);

            // 3. Validar que los datos del Basket sean consistentes.
            foreach (var item in basket.Items)
            {
                if (item.Quantity <= 0)
                    throw new InconsistentBasketDataException($"El producto '{item.ProductName}' tiene una cantidad inválida ({item.Quantity}).");
                if (item.Price < 0)
                    throw new InconsistentBasketDataException($"El producto '{item.ProductName}' tiene un precio inválido ({item.Price}).");
                if (string.IsNullOrWhiteSpace(item.ProductName))
                    throw new InconsistentBasketDataException($"El producto con id '{item.ProductId}' no tiene nombre.");
            }

            // 4. Armar la orden conservando el precio del Basket (precio al momento de la compra).
            var items = basket.Items
                .Select(i => new OrderItem
                {
                    ProductId = i.ProductId,
                    ProductName = i.ProductName,
                    Quantity = i.Quantity,
                    UnitPrice = i.Price
                })
                .ToList();

            var subtotal = Math.Round(items.Sum(i => i.LineTotal), 2);
            var taxRate = configuration.GetValue<decimal?>("Orders:TaxRate") ?? 0.16m;
            var tax = Math.Round(subtotal * taxRate, 2);
            var total = subtotal + tax;

            var order = new Order
            {
                Id = Guid.NewGuid(),
                CustomerId = command.CustomerId,
                CreatedAt = DateTime.UtcNow,
                Status = OrderStatus.Pending,
                Items = items,
                Subtotal = subtotal,
                Tax = tax,
                Total = total,
                IdempotencyKey = command.IdempotencyKey
            };

            // 5. Guardar. Si otro request concurrente ganó la carrera con el mismo Idempotency-Key,
            // TryCreateAsync devuelve false y recuperamos esa orden en vez de fallar.
            var created = await orderRepository.TryCreateAsync(order, cancellationToken);
            if (!created)
            {
                var raceWinnerOrder = await orderRepository.GetByIdempotencyKeyAsync(command.IdempotencyKey, cancellationToken);
                return new CreateOrderResult(raceWinnerOrder!, AlreadyExisted: true);
            }

            return new CreateOrderResult(order, AlreadyExisted: false);
        }
    }
}
