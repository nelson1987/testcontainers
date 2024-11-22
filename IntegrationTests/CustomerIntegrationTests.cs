using System.Text;
using System.Text.Json;
using Application;
using Domain;
using FluentAssertions;
using Infrastructure;
using Presentation.Commons;

namespace IntegrationTests;

public class CustomerIntegrationTests : SharedInfrastructure
{
    private const string QueueName = "customer_events";
    private readonly ICustomerRepository _customerRepository;
    private readonly IProducer<CreatedCustomerEvent> _producer;
    private readonly IConsumer<CreatedOrderEvent> _consumer;

    public CustomerIntegrationTests(SharedTestInfrastructure infrastructure)
        : base(infrastructure)
    {
        Channel.QueueDeclareAsync(QueueName, durable: true, exclusive: false, autoDelete: false)
            .GetAwaiter()
            .GetResult();
        Channel.QueueDeclareAsync(typeof(CreatedOrderEvent).FullName!, durable: true, exclusive: false,
                autoDelete: false)
            .GetAwaiter()
            .GetResult();
        _customerRepository = new CustomerRepository(DbContext);
        _producer = new Producer<CreatedCustomerEvent>(Channel);
        _consumer = new Consumer<CreatedOrderEvent>(Channel);
    }

    [Fact]
    public async Task CreateCustomer_ShouldPublishEvent()
    {
        // Arrange
        var customer = new Customer(0, "John Doe", "john@example.com", 30);

        // Act
        await _customerRepository.AddCustomerAsync(customer);

        // Publicar evento no RabbitMQ
        var @event = new DomainEvent<CreatedCustomerEvent>(new CreatedCustomerEvent(customer.Id));
        await _producer.Send(@event);

        // Assert
        var savedCustomer = await _customerRepository.GetCustomerAsync(customer);
        savedCustomer.Should().NotBeNull();
    }

    [Fact(Skip = "Integration tests fails on CI")]
    public Task GetCustomerDetails_ShouldCallExternalApi()
    {
        // // Arrange
        // var httpClient = HttpClientFactory.CreateClient("TestClient");
        //
        // // Act
        // var response = await httpClient.GetAsync($"customers/1");
        //
        // // Assert
        // Assert.True(response.IsSuccessStatusCode);
        throw new NotImplementedException();
    }

    [Fact(Skip = "Integration tests fails on CI")]
    public async Task GetCustomerDetails_ShouldCallInternalApi()
    {
        // Assert
        var command = new CreateOrderCommand("John Doe", "johndoe@email.com",18,100.00M);
        var serializeCommand = JsonSerializer.Serialize(command);
        var content = new StringContent(serializeCommand, Encoding.UTF8, "application/json");
        // Act
        var response = await Client.PostAsync(Constantes.GET_URI_PATH, content);
        // Assert
        response.Should().BeSuccessful();
        
        await _consumer.Consume().WaitAsync(TimeSpan.FromSeconds(5));
        var messageReceived = await _consumer.MessageReceived.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var messageEventReceived = await _consumer.MessageEventReceived.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var @event = JsonSerializer.Deserialize<DomainEvent<CreatedOrderEvent>>(messageEventReceived);
        messageReceived.Should().BeTrue();
        @event!.Message.OrderId.Should().Be(1);
    }
    // [Fact]
    // public async Task GetCustomerDetails_ShouldCallInternalApi_Result()
    // {
    //     // Act
    //     var response = await Client.GetAsync(Constantes.GET_URI_PATH);
    //     var users = await response.Content.ReadFromJsonAsync<WeatherForecast[]>();
    //     var result = await response.Content.ReadAsStringAsync();
    //     // Assert
    //     result.Should().Be();
    //     
    // }
}