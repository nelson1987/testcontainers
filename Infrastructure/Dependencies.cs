using Domain;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

public static class Dependencies
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)//, IConfiguration configuration)
    {
        services.AddScoped(typeof(IConsumer<>), typeof(Consumer<>));
        services.AddScoped(typeof(IProducer<>), typeof(Producer<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        return services;
    }
}