using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Presentation.Commons;
using RabbitMQ.Client;
using Testcontainers.RabbitMq;

namespace IntegrationTests;

public class RabbitMqIntegrationTests
{
    private readonly RabbitMqContainer _rabbitMqContainer = new RabbitMqBuilder()
        .WithImage("rabbitmq:3-alpine")
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
        var mock = new Mock<ILogger<Publisher<CreatedUserEvent>>>();
        var publisher = new Publisher<CreatedUserEvent>(_channel, mock.Object);
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