using System.Text;
using System.Text.Json;
using Application;
using Domain;
using FluentAssertions;
using Presentation.Commons;

namespace IntegrationTests;

public class OrderControllerIntegrationTests : SharedInfrastructure
{
    public OrderControllerIntegrationTests(SharedTestInfrastructure infrastructure) 
        : base(infrastructure)
    {
    }

    // [Fact]
    // public async Task GetCustomerDetails_ShouldCallInternalApi()
    // {
    //     // Assert
    //     var command = new CreateOrderCommand("John Doe", "johndoe@email.com",18,100.00M);
    //     var serializeCommand = JsonSerializer.Serialize(command);
    //     var content = new StringContent(serializeCommand, Encoding.UTF8, "application/json");
    //     // Act
    //     var response = await Client.PostAsync(Constantes.GET_URI_PATH, content);
    //     // Assert
    //     response.Should().BeSuccessful();
    //     
    //     await CreatedOrderConsumer.Consume().WaitAsync(TimeSpan.FromSeconds(5));
    //     //var messageReceived = await CreatedOrderConsumer.MessageReceived.Task.WaitAsync(TimeSpan.FromSeconds(5));
    //     var messageEventReceived = await CreatedOrderConsumer.MessageEventReceived.Task.WaitAsync(TimeSpan.FromSeconds(5));
    //     var @event = JsonSerializer.Deserialize<DomainEvent<CreatedOrderEvent>>(messageEventReceived);
    //     //messageReceived.Should().BeTrue();
    //     @event!.Message.OrderId.Should().Be(1);
    // }
}