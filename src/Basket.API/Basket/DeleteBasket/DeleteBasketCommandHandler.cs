using Basket.API.Data;
using FluentValidation;

namespace Basket.API.Basket.DeleteBasket
{
    public record DeleteBasketCommand(string UserName): ICommand<DeleteBasketResult>;

    public record DeleteBasketResult(bool isSuccess);
    public class DeleteBasketCommandValidator : AbstractValidator<DeleteBasketCommand>
    {
        public DeleteBasketCommandValidator()
        {
            RuleFor(x => x.UserName).NotEmpty().WithMessage("El nombre de usuario es requerido");
        }
    }
    public class DeleteBasketCommandHandler(IBasketRepository repository)
        : ICommandHandler<DeleteBasketCommand, DeleteBasketResult>
    {
        public async Task<DeleteBasketResult> Handle(DeleteBasketCommand command, CancellationToken cancellationToken)
        { 
        await repository.DeleteBasket(command.UserName, cancellationToken);
            return new DeleteBasketResult(true);
        }
    }
}
