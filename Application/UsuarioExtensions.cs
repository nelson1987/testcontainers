using Domain;

namespace Application;

public static class UsuarioExtensions
{
    public static Order ToEntity(this CreateOrderCommand command)
    {
        var customer = new Customer(0, command.CustomerName, command.CustomerEmail, command.CustomerAge);
        return new Order(0, DateTime.UtcNow, command.OrderTotal, customer);
    }
}