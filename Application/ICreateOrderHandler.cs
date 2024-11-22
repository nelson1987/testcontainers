namespace Application;

public interface ICreateOrderHandler
{
    Task Handle(CreateOrderCommand command, CancellationToken cancellationToken);
}