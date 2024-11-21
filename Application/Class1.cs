using Domain;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

public static class Dependencies
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IValidator<CreateOrderCommand>, CreateOrderValidator>();
        return services;
    }
}

public record CreateOrderCommand(string CustomerName, string CustomerEmail, int CustomerAge, decimal OrderTotal);

public static class UsuarioExtensions
{
    public static Order ToEntity(this CreateOrderCommand command)
    {
        var customer = new Customer(0, command.CustomerName, command.CustomerEmail, command.CustomerAge);
        return new Order(0, DateTime.UtcNow, command.OrderTotal, customer);
    }
}

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
public class CreateOrderHandler
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