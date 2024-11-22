namespace Domain;

public interface IProducer<T> where T : class
{
    Task Send(DomainEvent<T> message);
}