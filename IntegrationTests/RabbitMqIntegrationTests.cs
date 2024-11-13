using FluentAssertions;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Testcontainers.RabbitMq;

namespace IntegrationTests;
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
    }
    [Fact]
    public async Task TestPublishAndConsumeMessage()
    {
        await _rabbitMqContainer.StartAsync();
        
        var connectionFactory = new ConnectionFactory();
        connectionFactory.Uri = new Uri(_rabbitMqContainer.GetConnectionString());

        // When
        using var connection = await connectionFactory.CreateConnectionAsync();
        using var _channel = await connection.CreateChannelAsync();
        
        // Arrange
        var queueName = "test-queue";
        var message = "Hello, RabbitMQ!";
        var messageReceived = false;

        await _channel.QueueDeclareAsync(queueName, durable: true, exclusive: false, autoDelete: false);

        // Act
        // Publica a mensagem
        var body = System.Text.Encoding.UTF8.GetBytes(message);
        await _channel.BasicPublishAsync(exchange: "", routingKey: queueName, body: body);

        // Configura o consumer
        var consumer = new AsyncEventingBasicConsumer(_channel);
        var receivedMessage = string.Empty;

        consumer.ReceivedAsync  += async (model, ea) =>
        {
            var receivedBody = ea.Body.ToArray();
            receivedMessage = System.Text.Encoding.UTF8.GetString(receivedBody);
            messageReceived = true;
            await _channel.BasicAckAsync(ea.DeliveryTag, false);
        };

        await _channel.BasicConsumeAsync(queue: queueName,
            autoAck: false,
            consumer: consumer);

        // Aguarda o processamento da mensagem
        await Task.Delay(1000); // Tempo para processamento

        // Assert
        Assert.True(messageReceived);
        Assert.Equal(message, receivedMessage);
        
        await _rabbitMqContainer.StopAsync();

        // ASSERT
        //user.Id.Should().Be(1);
    }
}