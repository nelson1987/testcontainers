using Domain;
using FluentValidation;

namespace Application;

public class CreateOrderHandler : ICreateOrderHandler
{
    private readonly IOrderDomainService _orderDomainService;
    private readonly IValidator<CreateOrderCommand> _validator;

    public CreateOrderHandler(IOrderDomainService orderDomainService,
        IValidator<CreateOrderCommand> validator)
    {
        _orderDomainService = orderDomainService;
        _validator = validator;
    }

    public async Task Handle(CreateOrderCommand command, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (validationResult.IsValid)
            await _orderDomainService.AddOrderAsync(command.ToEntity());
    }
}