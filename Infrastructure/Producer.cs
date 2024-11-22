using System.Text;
using System.Text.Json;
using Domain;
using RabbitMQ.Client;

namespace Infrastructure;

public class Producer<T> : IProducer<T> where T : class
{
    private readonly IChannel _channel;

    public Producer(IChannel channel)
    {
        _channel = channel;
    }

    public async Task Send(DomainEvent<T> message)
    {
        var messageJson = JsonSerializer.Serialize(message);
        var messageBody = Encoding.UTF8.GetBytes(messageJson);
        await Send(message.EventType, messageBody);
    }

    public async Task Send(string queueName, byte[] message)
    {
        var properties = new BasicProperties
        {
            Persistent = true
        };
        await _channel.BasicPublishAsync(exchange: string.Empty,
            routingKey: queueName,
            mandatory: true,
            basicProperties: properties,
            body: message);
    }
}