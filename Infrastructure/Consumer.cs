using System.Text;
using Domain;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Infrastructure;

public class Consumer<T> : IConsumer<T> where T : class
{
    public TaskCompletionSource<bool> MessageReceived { get; }
    public TaskCompletionSource<string> MessageEventReceived { get; }

    private readonly IChannel _channel;

    public Consumer(IChannel channel)
    {
        _channel = channel;
        MessageReceived = new();
        MessageEventReceived = new();
    }

    public async Task Consume(string queueName)
    {
        var consumerEvent = new AsyncEventingBasicConsumer(_channel);
        consumerEvent.ReceivedAsync += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            MessageReceived.SetResult(true);
            MessageEventReceived.SetResult(message);
            await _channel.BasicAckAsync(ea.DeliveryTag, false);
        };

        // Act
        await _channel.BasicConsumeAsync(queue: queueName,
            autoAck: false,
            consumer: consumerEvent);
    }

    public async Task Consume()
    {
        await Consume(typeof(T).FullName!);
    }
}