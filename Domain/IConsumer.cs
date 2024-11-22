namespace Domain;

public interface IConsumer<T> where T : class
{
    TaskCompletionSource<bool> MessageReceived { get; }
    TaskCompletionSource<string> MessageEventReceived { get; }
    Task Consume(string queueName);
    Task Consume();
}