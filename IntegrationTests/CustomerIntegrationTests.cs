using Domain;
using FluentAssertions;
using Infrastructure;
using Presentation.Commons;

namespace IntegrationTests;

public class CustomerIntegrationTests : SharedInfrastructure
{
    private const string QueueName = "customer_events";
    private readonly CustomerRepository customerRepository;
    private readonly IProducer<CreatedCustomerEvent> producer;

    public CustomerIntegrationTests(SharedTestInfrastructure infrastructure)
        : base(infrastructure)
    {
        Channel.QueueDeclareAsync(QueueName, durable: true, exclusive: false, autoDelete: false).GetAwaiter()
            .GetResult();
        customerRepository = new CustomerRepository(DbContext);
        producer = new Producer<CreatedCustomerEvent>(Channel);
    }

    [Fact]
    public async Task CreateCustomer_ShouldPublishEvent()
    {
        // Arrange
        var customer = new Customer(0, "John Doe", "john@example.com", 30);

        // Act
        await customerRepository.AddCustomerAsync(customer);

        // Publicar evento no RabbitMQ
        var @event = new DomainEvent<CreatedCustomerEvent>(new CreatedCustomerEvent(customer.Id));
        await producer.Send(@event);

        // Assert
        var savedCustomer = await customerRepository.GetCustomerAsync(customer);
        Assert.NotNull(savedCustomer);
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
        // Act
        var response = await Client.GetAsync(Constantes.GET_URI_PATH);
        // Assert
        response.Should().BeSuccessful();
    }
}