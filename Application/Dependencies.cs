using Domain;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

public static class Dependencies
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services
            .AddScoped<IValidator<CreateOrderCommand>, CreateOrderValidator>()
            .AddScoped<ICreateOrderHandler, CreateOrderHandler>()
            .AddScoped<IOrderDomainService, OrderDomainService>();
        return services;
    }
}