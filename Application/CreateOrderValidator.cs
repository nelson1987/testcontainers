using FluentValidation;

namespace Application;

public class CreateOrderValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderValidator()
    {
        RuleFor(x => x.CustomerName).NotEmpty();
        RuleFor(x => x.CustomerEmail).EmailAddress().NotEmpty();
        RuleFor(x => x.CustomerAge).GreaterThan(17);
        RuleFor(x => x.OrderTotal).GreaterThan(0);
    }
}