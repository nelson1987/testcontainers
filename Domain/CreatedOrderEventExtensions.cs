namespace Domain;

public static class CreatedOrderEventExtensions
{
    public static DomainEvent<CreatedOrderEvent> ToCreatedEvent(this Order order)
        => new DomainEvent<CreatedOrderEvent>(new CreatedOrderEvent(order.Id));
}