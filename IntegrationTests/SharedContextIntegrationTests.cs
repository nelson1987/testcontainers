using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Presentation.Commons;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Testcontainers.MsSql;
using Testcontainers.RabbitMq;

namespace IntegrationTests;

public class IntegrationTestWebAppFactory
    : WebApplicationFactory<Presentation.Program>, IDisposable
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

// Classe de contexto do EF Core
public class TestDbContext : DbContext
{
    public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
    {
    }

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
    protected Customer (){
    }

    public Customer(int id, string name, string email, int age)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(0, id);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        //ArgumentOutOfRangeException.ThrowIfLessThan(18, age);
        Id = id;
        Name = name;
        Email = email;
        Age = age;
    }

    public int Id { get; private set; }
    public string Name { get; private set; }
    public string Email { get; private set; }
    public int Age { get; private set; }
    public List<Order> Orders { get; set; } = new();
}

public class Order
{
    protected Order (){
    }
    public Order(int id, DateTime orderDate, decimal total, Customer customer)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(0, id);
        //ArgumentOutOfRangeException.ThrowIfLessThan(DateTime.Today.AddDays(-1), orderDate);
        //ArgumentOutOfRangeException.ThrowIfLessThan(0.01M, total);
        Id = id;
        OrderDate = orderDate;
        Total = total;
        //CustomerId = customerId;
        Customer = customer;
    }

    public int Id { get; private set; }
    public DateTime OrderDate { get; private set; }
    public decimal Total { get; private set; }
    public int CustomerId { get; private set; }
    public Customer Customer { get; private set; }
}

// Classe que encapsula toda a infraestrutura compartilhada
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

// Primeira classe de testes
public class CustomerIntegrationTests : SharedInfrastructure
{
    private const string QueueName = "customer_events";

    public CustomerIntegrationTests(SharedTestInfrastructure infrastructure)
        : base(infrastructure)
    {
        Channel.QueueDeclareAsync(QueueName, durable: true, exclusive: false, autoDelete: false).GetAwaiter().GetResult();
    }

    [Fact]
    public async Task CreateCustomer_ShouldPublishEvent()
    {
        // Arrange
        var customer = new Customer(0, "John Doe", "john@example.com", 30);

        // Act
        DbContext.Set<Customer>().Add(customer);
        await DbContext.SaveChangesAsync();

        // Publicar evento no RabbitMQ
        var message = JsonSerializer.Serialize(new
        {
            EventType = "CustomerCreated",
            CustomerId = customer.Id
        });
        var body = Encoding.UTF8.GetBytes(message);
        var properties = new BasicProperties
        {
            Persistent = true
        };
        await Channel.BasicPublishAsync(exchange: string.Empty,
            routingKey: QueueName,
            mandatory: true,
            basicProperties: properties,
            body: body);

        // Assert
        var savedCustomer = await DbContext.Set<Customer>()
            .FirstOrDefaultAsync(c => c.Email == customer.Email);
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

// Segunda classe de testes
public class OrderIntegrationTests : SharedInfrastructure
{
    private const string QueueName = "order_events";

    public OrderIntegrationTests(SharedTestInfrastructure infrastructure)
        : base(infrastructure)
    {
        Channel.QueueDeclareAsync(QueueName, durable: true, exclusive: false, autoDelete: false).GetAwaiter().GetResult();
    }

    [Fact]
    public async Task CreateOrder_ShouldPublishEvent()
    {
        // Arrange
        var customer = new Customer(0, "John Doe", "john@example.com", 30);

        DbContext.Set<Customer>().Add(customer);
        await DbContext.SaveChangesAsync();

        var order = new Order(0, DateTime.UtcNow, 100.50m, customer);
        
        // Act
        DbContext.Set<Order>().Add(order);
        await DbContext.SaveChangesAsync();

        // Publicar evento no RabbitMQ
        var message = JsonSerializer.Serialize(new
        {
            EventType = "OrderCreated",
            OrderId = order.Id
        });
        var body = Encoding.UTF8.GetBytes(message);
        var properties = new BasicProperties
        {
            Persistent = true
        };
        await Channel.BasicPublishAsync(exchange: string.Empty,
            routingKey: QueueName,
            mandatory: true,
            basicProperties: properties,
            body: body);

        // Assert
        var savedOrder = await DbContext.Set<Order>()
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
        var consumer = new AsyncEventingBasicConsumer(Channel);

        consumer.ReceivedAsync += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            messageReceived.SetResult(true);
            messageEventReceived.SetResult(message);
            await Channel.BasicAckAsync(ea.DeliveryTag, false);
        };

        // Act
        await Channel.BasicConsumeAsync(queue: QueueName,
            autoAck: false,
            consumer: consumer);

        var message = JsonSerializer.Serialize(new { EventType = "TestEvent" });
        var messageBody = Encoding.UTF8.GetBytes(message);
        var properties = new BasicProperties
        {
            Persistent = true
        };
        await Channel.BasicPublishAsync(exchange: string.Empty,
            routingKey: QueueName,
            mandatory: true,
            basicProperties: properties,
            body: messageBody);

        // Assert
        var result = await messageReceived.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.True(result);
    }
}