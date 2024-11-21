using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Testcontainers.MsSql;
using Testcontainers.RabbitMq;

namespace IntegrationTests;

// Classe de contexto do EF Core
public class TestDbContext : DbContext
{
    public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
    {
    }

    public DbSet<Customer> Customers { get; set; }
    public DbSet<Order> Orders { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Age).IsRequired();
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OrderDate).IsRequired();
            entity.Property(e => e.Total).IsRequired().HasPrecision(18, 2);
            entity.HasOne(e => e.Customer)
                .WithMany(c => c.Orders)
                .HasForeignKey(e => e.CustomerId);
        });
    }
}
// Modelos
public class Customer
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public int Age { get; set; }
    public List<Order> Orders { get; set; } = new();
}

public class Order
{
    public int Id { get; set; }
    public DateTime OrderDate { get; set; }
    public decimal Total { get; set; }
    public int CustomerId { get; set; }
    public Customer Customer { get; set; }
}
// Classe que encapsula toda a infraestrutura compartilhada
public class SharedTestInfrastructure : IAsyncLifetime
{
    private readonly MsSqlContainer _sqlContainer;
    private readonly RabbitMqContainer _rabbitContainer;
    private readonly IServiceProvider _serviceProvider;
    private readonly HttpClient _httpClient;

    public string SqlConnectionString { get; private set; }
    public IConnection RabbitConnection { get; private set; }
    public IHttpClientFactory HttpClientFactory { get; private set; }

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
        var services = new ServiceCollection();
        services.AddHttpClient("TestClient", client =>
        {
            client.BaseAddress = new Uri("https://api.example.com/");
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });

        _serviceProvider = services.BuildServiceProvider();
        HttpClientFactory = _serviceProvider.GetRequiredService<IHttpClientFactory>();
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
    }

    public async Task DisposeAsync()
    {
        RabbitConnection?.Dispose();
        await Task.WhenAll(
            _sqlContainer.DisposeAsync().AsTask(),
            _rabbitContainer.DisposeAsync().AsTask()
        );
        if (_serviceProvider is IDisposable disposable)
            disposable.Dispose();
    }
}

// Coleção compartilhada para os testes
[CollectionDefinition("Shared Infrastructure")]
public class SharedInfrastructureCollection : ICollectionFixture<SharedTestInfrastructure>
{
}

// Classe base para testes que compartilham a infraestrutura
public abstract class IntegrationTestBase
{
    protected readonly TestDbContext DbContext;
    protected readonly IConnection RabbitConnection;
    protected readonly IHttpClientFactory HttpClientFactory;
    protected readonly SharedTestInfrastructure Infrastructure;

    protected IntegrationTestBase(SharedTestInfrastructure infrastructure)
    {
        Infrastructure = infrastructure;
        
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlServer(infrastructure.SqlConnectionString)
            .LogTo(Console.WriteLine, LogLevel.Information)
            .Options;
            
        DbContext = new TestDbContext(options);
        RabbitConnection = infrastructure.RabbitConnection;
        HttpClientFactory = infrastructure.HttpClientFactory;
    }
}

// Primeira classe de testes
[Collection("Shared Infrastructure")]
public class CustomerIntegrationTests : IntegrationTestBase, IAsyncLifetime
{
    private IChannel _channel;
    private const string QueueName = "customer_events";

    public CustomerIntegrationTests(SharedTestInfrastructure infrastructure)
        : base(infrastructure)
    {
        _channel = RabbitConnection.CreateChannelAsync().GetAwaiter().GetResult();
    }
    
    public async Task InitializeAsync()
    {
        // Configuração específica para testes de Order
        //_channel.QueueDeclare(QueueName, false, false, false, null);
        await _channel.QueueDeclareAsync(QueueName, durable: true, exclusive: false, autoDelete: false);
    }

    public async Task DisposeAsync()
    {
        await _channel.DisposeAsync();
    }

    [Fact]
    public async Task CreateCustomer_ShouldPublishEvent()
    {
        // Arrange
        var customer = new Customer
        {
            Name = "John Doe",
            Email = "john@example.com",
            Age = 30
        };

        // Act
        DbContext.Customers.Add(customer);
        await DbContext.SaveChangesAsync();

        // Publicar evento no RabbitMQ
        var message = JsonSerializer.Serialize(new { 
            EventType = "CustomerCreated", 
            CustomerId = customer.Id 
        });
        var body = Encoding.UTF8.GetBytes(message);
        var properties = new BasicProperties
        {
            Persistent = true
        };
        await _channel.BasicPublishAsync(exchange: string.Empty,
            routingKey: QueueName,
            mandatory: true,
            basicProperties: properties,
            body: body);

        // Assert
        var savedCustomer = await DbContext.Customers
            .FirstOrDefaultAsync(c => c.Email == "john@example.com");
        Assert.NotNull(savedCustomer);
    }

    [Fact]
    public async Task GetCustomerDetails_ShouldCallExternalApi()
    {
        // Arrange
        var httpClient = HttpClientFactory.CreateClient("TestClient");

        // Act
        var response = await httpClient.GetAsync($"customers/1");

        // Assert
        Assert.True(response.IsSuccessStatusCode);
    }
}

// Segunda classe de testes
[Collection("Shared Infrastructure")]
public class OrderIntegrationTests : IntegrationTestBase, IAsyncLifetime
{
    private IChannel _channel;
    private const string QueueName = "order_events";

    public OrderIntegrationTests(SharedTestInfrastructure infrastructure)
        : base(infrastructure)
    {
        //_channel = RabbitConnection.CreateModel();
        _channel = RabbitConnection.CreateChannelAsync().GetAwaiter().GetResult();
    }

    public async Task InitializeAsync()
    {
        // Configuração específica para testes de Order
        //_channel.QueueDeclare(QueueName, false, false, false, null);
        await _channel.QueueDeclareAsync(QueueName, durable: true, exclusive: false, autoDelete: false);
    }

    public async Task DisposeAsync()
    {
        await _channel.DisposeAsync();
    }

    [Fact]
    public async Task CreateOrder_ShouldPublishEvent()
    {
        // Arrange
        var customer = new Customer
        {
            Name = "Test Customer",
            Email = "test@example.com",
            Age = 30
        };
        DbContext.Customers.Add(customer);
        await DbContext.SaveChangesAsync();

        var order = new Order
        {
            CustomerId = customer.Id,
            OrderDate = DateTime.UtcNow,
            Total = 100.50m
        };

        // Act
        DbContext.Orders.Add(order);
        await DbContext.SaveChangesAsync();

        // Publicar evento no RabbitMQ
        var message = JsonSerializer.Serialize(new { 
            EventType = "OrderCreated", 
            OrderId = order.Id 
        });
        var body = Encoding.UTF8.GetBytes(message);
        var properties = new BasicProperties
        {
            Persistent = true
        };
        await _channel.BasicPublishAsync(exchange: string.Empty,
            routingKey: QueueName,
            mandatory: true,
            basicProperties: properties,
            body: body);

        // Assert
        var savedOrder = await DbContext.Orders
            .Include(o => o.Customer)
            .FirstOrDefaultAsync(o => o.Id == order.Id);
        Assert.NotNull(savedOrder);
    }

    [Fact]
    public async Task ProcessOrder_ShouldConsumeMessage()
    {
        // Arrange
        var messageReceived = new TaskCompletionSource<bool>();
        var messageEventReceived = new TaskCompletionSource<string>();
        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.ReceivedAsync += async(model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            messageReceived.SetResult(true);
            messageEventReceived.SetResult(message);
            await _channel.BasicAckAsync(ea.DeliveryTag, false);
        };

        // Act
        await _channel.BasicConsumeAsync(queue: QueueName,
            autoAck: false,
            consumer: consumer);

        var message = JsonSerializer.Serialize(new { EventType = "TestEvent" });
        var messageBody = Encoding.UTF8.GetBytes(message);
        var properties = new BasicProperties
        {
            Persistent = true
        };
        await _channel.BasicPublishAsync(exchange: string.Empty,
            routingKey: QueueName,
            mandatory: true,
            basicProperties: properties,
            body: messageBody);

        // Assert
        var result = await messageReceived.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.True(result);
    }
}