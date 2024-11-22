namespace Domain;

public class DomainEvent<T>
    where T : class
{
    public string EventType = typeof(T).FullName!;
    public string EventId = Guid.NewGuid().ToString("D");
    public DateTime EventDate = DateTime.UtcNow;

    public DomainEvent(T message)
    {
        Message = message;
    }

    public T Message { get; private set; }
}