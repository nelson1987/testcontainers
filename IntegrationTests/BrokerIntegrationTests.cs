using System.Text;
using System.Text.Json;
using FluentAssertions;
using Infrastructure;

namespace IntegrationTests;

public class BrokerIntegrationTests : SharedInfrastructure
{
    private const string QueueName = "broker_events";

    public BrokerIntegrationTests(SharedTestInfrastructure infrastructure)
        : base(infrastructure)
    {
        Channel.QueueDeclareAsync(QueueName, durable: true, exclusive: false, autoDelete: false)
            .GetAwaiter()
            .GetResult();
    }

    [Fact]
    public async Task ProcessOrder_ShouldConsumeMessage()
    {
        // Arrange
        var consumer = new Consumer<string>(Channel);
        var producer = new Producer<object>(Channel);
        var message = JsonSerializer.Serialize(new { EventType = "TestEvent" });
        var messageBody = Encoding.UTF8.GetBytes(message);
        await producer.Send(QueueName, messageBody);

        // Act
        await consumer.Consume(QueueName).WaitAsync(TimeSpan.FromSeconds(5));

        // Assert
        var messageReceived = await consumer.MessageReceived.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var messageEventReceived = await consumer.MessageEventReceived.Task.WaitAsync(TimeSpan.FromSeconds(5));
        messageReceived.Should().BeTrue();
        messageEventReceived.Should().Be("{\"EventType\":\"TestEvent\"}");
    }
}