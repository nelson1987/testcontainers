using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Presentation.Commons;

public class Subscriber<T> where T : class
{
    private readonly IChannel _channel;
    public bool messageReceived;
    public string receivedMessage;
    public T receivedEvent;
    public Subscriber(IChannel connection)
    {
        _channel = connection;
    }

    public async Task Consume(string queueName)
    {
        await _channel.QueueDeclareAsync(queueName, durable: true, exclusive: false, autoDelete: false);
        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.ReceivedAsync  += async (model, ea) =>
        {
            var receivedBody = ea.Body.ToArray();
            receivedMessage = System.Text.Encoding.UTF8.GetString(receivedBody);
            receivedEvent = System.Text.Json.JsonSerializer.Deserialize<T>(receivedMessage)!;
            messageReceived = true;
            await _channel.BasicAckAsync(ea.DeliveryTag, false);
        };

        await _channel.BasicConsumeAsync(queue: queueName,
            autoAck: false,
            consumer: consumer);
    }
}

