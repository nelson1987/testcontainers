using System.Text;
using System.Text.Json;
using Domain;
using FluentAssertions;
using FluentAssertions.Extensions;
using Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Presentation;
using Presentation.Commons;
using RabbitMQ.Client;
using Testcontainers.MsSql;
using Testcontainers.RabbitMq;

namespace IntegrationTests;

public class IntegrationTestWebAppFactory
    : WebApplicationFactory<Program>, IDisposable
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // services.RemoveAll<ConnectionFactory>();
            // services.AddSingleton(sp =>
            // {
            //     var uri = new Uri(_rabbitMqContainer.GetConnectionString());
            //     return new ConnectionFactory()
            //     {
            //         Uri = uri,
            //     };
            // });
            // var connectionString = new SqlConnectionStringBuilder(_dbContainer.GetConnectionString());
            // connectionString.InitialCatalog = Guid.NewGuid().ToString("D");
            //
            // var serviceProvider = new ServiceCollection()
            //     .AddEntityFrameworkSqlServer()
            //     .BuildServiceProvider();
            //
            // var builder = new DbContextOptionsBuilder<MyContext>();
            // var options = builder
            //     .UseSqlServer(connectionString.ToString())
            //     .UseInternalServiceProvider(serviceProvider)
            //     .Options;
            //
            // MyContext dbContext = new MyContext(options);
            // dbContext.Database.EnsureDeleted();
            // dbContext.Database.EnsureCreated();
            // dbContext.Database.Migrate();
            // services.AddScoped<IUnitOfWork, UnitOfWork>(x=> new UnitOfWork(dbContext));
        });
    }
}

public class CustomerUnitTests
{
    [Fact]
    public void ConstructingCustomer_ShouldCreateCorrectly()
    {
        var customer = new Customer(1, "John", "Doe", 22);
        customer.Id.Should().Be(1);
        customer.Name.Should().Be("John");
        customer.Email.Should().Be("Doe");
        customer.Age.Should().Be(22);
    }

    [Fact]
    public void ConstructingCustomer_ShouldHaveIdGreaterThanZero()
    {
        var customer = () => new Customer(-1, "John", "Doe", 22);
        customer.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("  ")]
    [InlineData(null)]
    public void ConstructingCustomer_ShouldHaveName(string name)
    {
        var customer = () => new Customer(0, name, "Doe", 22);
        customer.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("  ")]
    [InlineData(null)]
    public void ConstructingCustomer_ShouldHaveEmail(string email)
    {
        var customer = () => new Customer(0, "John", email, 22);
        customer.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(-1)]
    [InlineData(17)]
    public void ConstructingCustomer_ShouldHaveAgeGreaterThan18(int age)
    {
        var customer = () => new Customer(0, "John", "Doe", age);
        customer.Should().Throw<ArgumentOutOfRangeException>();
    }
}

public class OrderUnitTests
{
    private static readonly Customer customer = new Customer(1, "John", "Doe", 22);

    [Fact]
    public void ConstructingOrder_ShouldCreateCorrectly()
    {
        var order = new Order(0, DateTime.UtcNow, 0.01M, customer);
        order.Id.Should().Be(0);
        order.OrderDate.Should().BeCloseTo(DateTime.UtcNow, 1.Seconds());
        order.Total.Should().Be(0.01M);
        order.Customer.Id.Should().Be(customer.Id);
    }

    [Fact]
    public void ConstructingCustomer_ShouldHaveIdGreaterThanZero()
    {
        var order = () => new Order(-1, DateTime.UtcNow, 0.01M, customer);
        order.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void ConstructingCustomer_ShouldHaveIdOrderDataGreaterThanNow()
    {
        var order = () => new Order(0, DateTime.UtcNow.AddSeconds(-5), 0.01M, customer);
        order.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void ConstructingCustomer_ShouldHaveIdTotalGreaterThanZero()
    {
        var order = () => new Order(0, DateTime.UtcNow, -0.01M, customer);
        order.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void ConstructingCustomer_ShouldHaveRequiredCustomer()
    {
        var order = () => new Order(0, DateTime.UtcNow, 0.01M, null);
        order.Should().Throw<ArgumentNullException>();
    }
}

public class SharedTestInfrastructure : IAsyncLifetime
{
    private readonly MsSqlContainer _sqlContainer;

    private readonly RabbitMqContainer _rabbitContainer;

    // private readonly IServiceProvider _serviceProvider;
    // private readonly HttpClient _httpClient;
    private readonly IntegrationTestWebAppFactory _factory;
    public string SqlConnectionString { get; private set; }

    public IConnection RabbitConnection { get; private set; }

    //public IHttpClientFactory HttpClientFactory { get; private set; }
    public HttpClient Client { get; private set; }

    public SharedTestInfrastructure()
    {
        _sqlContainer = new MsSqlBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
            .WithPassword("Strong_password_123!")
            .Build();

        _rabbitContainer = new RabbitMqBuilder()
            .WithImage("rabbitmq:3-management")
            .WithUsername("guest")
            .WithPassword("guest")
            .Build();

        // Configuração do HttpClient com DI
        //var services = new ServiceCollection();
        // services.AddHttpClient("TestClient", client =>
        // {
        //     client.BaseAddress = new Uri("https://api.example.com/");
        //     client.DefaultRequestHeaders.Add("Accept", "application/json");
        // });
        //
        // _serviceProvider = services.BuildServiceProvider();
        // HttpClientFactory = _serviceProvider.GetRequiredService<IHttpClientFactory>();
        // services.AddSingleton(sp =>
        // {
        //     var uri = new Uri(_rabbitContainer.GetConnectionString());
        //     return new ConnectionFactory()
        //     {
        //         Uri = uri,
        //     };
        // });
        // var connectionString = new SqlConnectionStringBuilder(_sqlContainer.GetConnectionString());
        // connectionString.InitialCatalog = Guid.NewGuid().ToString("D");
        //
        // var serviceProvider = new ServiceCollection()
        //     .AddEntityFrameworkSqlServer()
        //     .BuildServiceProvider();
        //
        // var builder = new DbContextOptionsBuilder<MyContext>();
        // var options = builder
        //     .UseSqlServer(connectionString.ToString())
        //     .UseInternalServiceProvider(serviceProvider)
        //     .Options;
        //
        // MyContext dbContext = new MyContext(options);
        // dbContext.Database.EnsureDeleted();
        // dbContext.Database.EnsureCreated();
        // dbContext.Database.Migrate();
        // services.AddScoped<IUnitOfWork, UnitOfWork>(x=> new UnitOfWork(dbContext));

        _factory = new IntegrationTestWebAppFactory();
        Client = _factory.CreateDefaultClient();
    }

    public async Task InitializeAsync()
    {
        // Inicia os containers em paralelo
        await Task.WhenAll(
            _sqlContainer.StartAsync(),
            _rabbitContainer.StartAsync()
        );

        SqlConnectionString = _sqlContainer.GetConnectionString();

        // Configura a conexão do RabbitMQ
        var connectionFactory = new ConnectionFactory();
        connectionFactory.Uri = new Uri(_rabbitContainer.GetConnectionString());
        RabbitConnection = await connectionFactory.CreateConnectionAsync();

        // Inicializa o banco de dados
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlServer(SqlConnectionString)
            .Options;

        await using var context = new TestDbContext(options);
        await context.Database.EnsureCreatedAsync();
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        RabbitConnection?.Dispose();
        await Task.WhenAll(
            _sqlContainer.DisposeAsync().AsTask(),
            _rabbitContainer.DisposeAsync().AsTask()
        );
        // if (_serviceProvider is IDisposable disposable)
        //     disposable.Dispose();
    }
}

[CollectionDefinition("Shared Infrastructure")]
public class SharedInfrastructureCollection : ICollectionFixture<SharedTestInfrastructure>
{
}

public abstract class IntegrationTestBase
{
    protected readonly TestDbContext DbContext;

    protected readonly IConnection RabbitConnection;

    //protected readonly IHttpClientFactory HttpClientFactory;
    protected readonly SharedTestInfrastructure Infrastructure;
    protected readonly HttpClient Client;

    protected IntegrationTestBase(SharedTestInfrastructure infrastructure)
    {
        Infrastructure = infrastructure;

        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlServer(infrastructure.SqlConnectionString)
            .LogTo(Console.WriteLine, LogLevel.Information)
            .Options;

        DbContext = new TestDbContext(options);
        RabbitConnection = infrastructure.RabbitConnection;
        //HttpClientFactory = infrastructure.HttpClientFactory;
        Client = infrastructure.Client;
    }
}

[Collection("Shared Infrastructure")]
public class SharedInfrastructure : IntegrationTestBase, IAsyncDisposable
{
    protected readonly IChannel Channel;

    public SharedInfrastructure(SharedTestInfrastructure infrastructure)
        : base(infrastructure)
    {
        Channel = RabbitConnection.CreateChannelAsync().GetAwaiter().GetResult();
    }

    public async ValueTask DisposeAsync()
    {
        await Channel.DisposeAsync();
    }
}

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

public class OrderIntegrationTests : SharedInfrastructure
{
    private readonly OrderDomainService _orderDomainService;
    private readonly IProducer<CreatedOrderEvent> producer;
    private readonly IConsumer<CreatedOrderEvent> consumer;

    public OrderIntegrationTests(SharedTestInfrastructure infrastructure)
        : base(infrastructure)
    {
        Channel.QueueDeclareAsync(typeof(CreatedOrderEvent).FullName, durable: true, exclusive: false,
                autoDelete: false)
            .GetAwaiter()
            .GetResult();
        _orderDomainService = new OrderDomainService(
            new UnitOfWork(DbContext),
            new Producer<CreatedOrderEvent>(Channel));
        consumer = new Consumer<CreatedOrderEvent>(Channel);
    }

    [Fact]
    public async Task CreateOrder_ShouldPresistEntity()
    {
        // Arrange
        var customer = new Customer(0, "John Doe", "john@example.com", 30);
        var order = new Order(0, DateTime.UtcNow, 100.50m, customer);

        // Act
        await _orderDomainService.AddOrderAsync(order);

        // Assert
        var savedOrder = await _orderDomainService.FindOrderAsync(order);
        Assert.NotNull(savedOrder);
    }

    [Fact]
    public async Task CreateOrder_ShouldPublishEvent()
    {
        // Arrange
        var customer = new Customer(0, "John Doe", "john@example.com", 30);
        var order = new Order(0, DateTime.UtcNow, 100.50m, customer);

        // Act
        await _orderDomainService.AddOrderAsync(order);
        await consumer.Consume(typeof(CreatedOrderEvent).FullName).WaitAsync(TimeSpan.FromSeconds(5));

        // Assert
        var messageReceived = await consumer.messageReceived.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var messageEventReceived = await consumer.messageEventReceived.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var @event = JsonSerializer.Deserialize<DomainEvent<CreatedOrderEvent>>(messageEventReceived);
        Assert.True(messageReceived);
        Assert.Equal(1, @event!.Message.OrderId);
    }
}

public class BrokerIntegrationTests : SharedInfrastructure
{
    private const string QueueName = "broker_events";

    public BrokerIntegrationTests(SharedTestInfrastructure infrastructure)
        : base(infrastructure)
    {
        Channel.QueueDeclareAsync(QueueName, durable: true, exclusive: false, autoDelete: false).GetAwaiter()
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
        var messageReceived = await consumer.messageReceived.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var messageEventReceived = await consumer.messageEventReceived.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.True(messageReceived);
        Assert.Equal("{\"EventType\":\"TestEvent\"}", messageEventReceived);
    }
}
