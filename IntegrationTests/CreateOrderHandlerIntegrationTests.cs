using System.Text.Json;
using Application;
using Domain;
using FluentAssertions;
using Infrastructure;

namespace IntegrationTests;

public class CreateOrderHandlerIntegrationTests : SharedInfrastructure
{
    private readonly CreateOrderHandler _handler;
    private readonly IConsumer<CreatedOrderEvent> _consumer;

    public CreateOrderHandlerIntegrationTests(SharedTestInfrastructure infrastructure)
        : base(infrastructure)
    {
        Channel.QueueDeclareAsync(typeof(CreatedOrderEvent).FullName!, durable: true, exclusive: false,
                autoDelete: false)
            .GetAwaiter()
            .GetResult();
        _handler = new CreateOrderHandler(new OrderDomainService(
                new UnitOfWork(DbContext),
                new Producer<CreatedOrderEvent>(Channel)
            ),
            new CreateOrderValidator()
        );
        _consumer = new Consumer<CreatedOrderEvent>(Channel);
    }
    
    [Fact]
    public async Task CreateOrder_ShouldPresistEntity()
    {
        // Arrange
        var command = new CreateOrderCommand("John Doe", "john@example.com", 30, 100.50m);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _consumer.Consume().WaitAsync(TimeSpan.FromSeconds(5));

        // Assert
        var messageReceived = await _consumer.MessageReceived.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var messageEventReceived = await _consumer.MessageEventReceived.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var @event = JsonSerializer.Deserialize<DomainEvent<CreatedOrderEvent>>(messageEventReceived);
        messageReceived.Should().BeTrue();
        @event!.Message.OrderId.Should().Be(1);
    }
}