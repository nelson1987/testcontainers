using System.Text.Json;
using Domain;
using FluentAssertions;
using Infrastructure;

namespace IntegrationTests;

public class OrderIntegrationTests : SharedInfrastructure
{
    private readonly IOrderDomainService _orderDomainService;
    private readonly IConsumer<CreatedOrderEvent> _consumer;

    public OrderIntegrationTests(SharedTestInfrastructure infrastructure)
        : base(infrastructure)
    {
        Channel.QueueDeclareAsync(typeof(CreatedOrderEvent).FullName!, durable: true, exclusive: false,
                autoDelete: false, arguments: null)
            .GetAwaiter()
            .GetResult();
        _orderDomainService = new OrderDomainService(
            new UnitOfWork(DbContext),
            new Producer<CreatedOrderEvent>(Channel));
        _consumer = new Consumer<CreatedOrderEvent>(Channel);
    }

    // [Fact]
    // public async Task CreateOrder_ShouldPersistEntity()
    // {
    //     // Arrange
    //     var customer = new Customer(0, "John Doe", "john@example.com", 30);
    //     var order = new Order(0, DateTime.UtcNow, 100.50m, customer);
    //
    //     // Act
    //     await _orderDomainService.AddOrderAsync(order);
    //
    //     // Assert
    //     var savedOrder = await _orderDomainService.FindOrderAsync(order);
    //     savedOrder.Should().NotBeNull();
    // }

    [Fact]
    public async Task CreateOrder_ShouldPublishEvent()
    {
        // Arrange
        var customer = new Customer(0, "John Doe", "john@example.com", 30);
        var order = new Order(0, DateTime.UtcNow, 100.50m, customer);

        // Act
        await _orderDomainService.AddOrderAsync(order);
        await _consumer.Consume().WaitAsync(TimeSpan.FromSeconds(5));

        // Assert
        var messageReceived = await _consumer.MessageReceived.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var messageEventReceived = await _consumer.MessageEventReceived.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var @event = JsonSerializer.Deserialize<DomainEvent<CreatedOrderEvent>>(messageEventReceived);
        messageReceived.Should().BeTrue();
        @event!.Message.OrderId.Should().Be(1);
    }
}