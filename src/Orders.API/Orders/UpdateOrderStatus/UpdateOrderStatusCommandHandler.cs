using FluentValidation;
using Orders.API.Data;
using Orders.API.Models;

namespace Orders.API.Orders.UpdateOrderStatus
{
    public record UpdateOrderStatusCommand(Guid Id, string Status) : ICommand<UpdateOrderStatusResult>;

    public record UpdateOrderStatusResult(Order Order);

    public class UpdateOrderStatusCommandValidator : AbstractValidator<UpdateOrderStatusCommand>
    {
        public UpdateOrderStatusCommandValidator()
        {
            RuleFor(x => x.Status)
                .NotEmpty().WithMessage("Status es requerido.")
                .Must(s => Enum.TryParse<OrderStatus>(s, ignoreCase: true, out _))
                .WithMessage("Status debe ser 'Confirmed' o 'Cancelled'.");
        }
    }

    public class UpdateOrderStatusCommandHandler(IOrderRepository orderRepository)
        : ICommandHandler<UpdateOrderStatusCommand, UpdateOrderStatusResult>
    {
        // Único par de transiciones permitido por el enunciado. Cancelled nunca regresa a Confirmed,
        // y de hecho ningún estado "final" (Confirmed o Cancelled) puede volver a moverse.
        private static readonly (OrderStatus From, OrderStatus To)[] AllowedTransitions =
        [
            (OrderStatus.Pending, OrderStatus.Confirmed),
            (OrderStatus.Pending, OrderStatus.Cancelled)
        ];

        public async Task<UpdateOrderStatusResult> Handle(UpdateOrderStatusCommand command, CancellationToken cancellationToken)
        {
            var order = await orderRepository.GetByIdAsync(command.Id, cancellationToken)
                ?? throw new OrderNotFoundException(command.Id);

            var newStatus = Enum.Parse<OrderStatus>(command.Status, ignoreCase: true);

            if (!AllowedTransitions.Contains((order.Status, newStatus)))
                throw new InvalidOrderStatusTransitionException(order.Status, newStatus);

            await orderRepository.UpdateStatusAsync(order.Id, newStatus, cancellationToken);
            order.Status = newStatus;

            return new UpdateOrderStatusResult(order);
        }
    }
}
