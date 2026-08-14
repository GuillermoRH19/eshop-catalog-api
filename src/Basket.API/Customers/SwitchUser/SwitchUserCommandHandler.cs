using Basket.API.Data;
using FluentValidation;

namespace Basket.API.Customers.SwitchUser
{
    public record SwitchUserCommand(string Name) : ICommand<SwitchUserResult>;

    public record SwitchUserResult(string Name, DateTime CreatedAt, bool IsNew);

    public class SwitchUserCommandValidator : AbstractValidator<SwitchUserCommand>
    {
        public SwitchUserCommandValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("El nombre de usuario es requerido.")
                .MaximumLength(60).WithMessage("El nombre de usuario no puede tener más de 60 caracteres.");
        }
    }

    public class SwitchUserCommandHandler(ICustomerRepository repository) : ICommandHandler<SwitchUserCommand, SwitchUserResult>
    {
        public async Task<SwitchUserResult> Handle(SwitchUserCommand command, CancellationToken cancellationToken)
        {
            var name = command.Name.Trim();
            var (customer, isNew) = await repository.GetOrCreateAsync(name, cancellationToken);
            return new SwitchUserResult(customer.Name, customer.CreatedAt, isNew);
        }
    }
}
