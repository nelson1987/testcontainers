using FluentAssertions;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Testcontainers.RabbitMq;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace IntegrationTests;

public record CreatedUserEvent(int Id, string Name, int Age, bool IsActive);

public class Publisher<T> where T : class
{
    private readonly IChannel _channel;

    public Publisher(IChannel connection)
    {
        _channel = connection;
    }

    public async Task Send(string queueName, T @event)
    {
        await _channel.QueueDeclareAsync(queueName, durable: true, exclusive: false, autoDelete: false);

        var message = JsonSerializer.Serialize(@event);
        var body = System.Text.Encoding.UTF8.GetBytes(message);
        var properties = new BasicProperties
        {
            Persistent = true
        };
        await _channel.BasicPublishAsync(exchange: string.Empty, 
            routingKey: queueName, 
            mandatory: true,
            basicProperties: properties,
            body: body);
    }
}
public class Subscriber<T> where T : class
{
    private readonly IChannel _channel;
    public bool messageReceived;
    public string receivedMessage;
    public CreatedUserEvent receivedEvent;
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
            receivedEvent = JsonConvert.DeserializeObject<CreatedUserEvent>(receivedMessage);
            messageReceived = true;
            await _channel.BasicAckAsync(ea.DeliveryTag, false);
        };

        await _channel.BasicConsumeAsync(queue: queueName,
            autoAck: false,
            consumer: consumer);
    }
}

public class RabbitMqIntegrationTests
{
    private readonly RabbitMqContainer _rabbitMqContainer = new RabbitMqBuilder()
        .WithImage("rabbitmq:3.11")
        .Build();

    [Fact]
    public async Task AddAsync_DadosValidos_CriarUsuarioComId()
    {
        await _rabbitMqContainer.StartAsync();

        // Given
        var connectionFactory = new ConnectionFactory();
        connectionFactory.Uri = new Uri(_rabbitMqContainer.GetConnectionString());

        // When
        using var connection = await connectionFactory.CreateConnectionAsync();
        using var channel = await connection.CreateChannelAsync();

        // Then
        connection.IsOpen.Should().BeTrue();
        
        await _rabbitMqContainer.StopAsync();
    }
    
    [Fact]
    public async Task TestPublishAndConsumeMessage()
    {
        await _rabbitMqContainer.StartAsync();
        
        var queueName = "test-queue";
        var @event = new CreatedUserEvent(6, "John Doe", 18, false);
        var connectionFactory = new ConnectionFactory();
        connectionFactory.Uri = new Uri(_rabbitMqContainer.GetConnectionString());

        using var connection = await connectionFactory.CreateConnectionAsync();
        using var _channel = await connection.CreateChannelAsync();
        
        //Act
        var publisher = new Publisher<CreatedUserEvent>(_channel);
        var subscriber = new Subscriber<CreatedUserEvent>(_channel);
        await publisher.Send(queueName, @event);
        await subscriber.Consume(queueName);

        // Aguarda o processamento da mensagem
        await Task.Delay(1000); // Tempo para processamento

        // Assert
        subscriber.messageReceived.Should().BeTrue();
        subscriber.receivedEvent.Should().Be(@event);
        
        await _rabbitMqContainer.StopAsync();
    }
}