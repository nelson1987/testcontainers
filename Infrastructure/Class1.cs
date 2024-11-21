using System.Reflection;
using System.Text;
using System.Text.Json;
using Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Infrastructure;

public static class Dependencies
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped(typeof(IConsumer<>), typeof(Consumer<>));
        services.AddScoped(typeof(IProducer<>), typeof(Producer<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        return services;
    }
}

public class TestDbContext : DbContext
{
    public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}

public class OrderMapConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("TB_ORDER")
            .HasKey(k => k.Id);
        builder.Property(e => e.OrderDate)
            .IsRequired();
        builder.Property(e => e.Total)
            .IsRequired()
            .HasPrecision(18, 2);
        builder.HasOne(e => e.Customer)
            .WithMany(c => c.Orders)
            .HasForeignKey(e => e.CustomerId);
    }
}

public class CustomerMapConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("TB_CUSTOMER")
            .HasKey(k => k.Id);
        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(100);
        builder.Property(e => e.Email)
            .IsRequired()
            .HasMaxLength(100);
        builder.Property(e => e.Age)
            .IsRequired();
    }
}

public class Consumer<T> : IConsumer<T> where T : class
{
    public TaskCompletionSource<bool> messageReceived { get; }
    public TaskCompletionSource<string> messageEventReceived { get; }

    private readonly IChannel Channel;

    public Consumer(IChannel channel)
    {
        Channel = channel;
        messageReceived = new();
        messageEventReceived = new();
    }


    public async Task Consume(string queueName)
    {
        var consumerEvent = new AsyncEventingBasicConsumer(Channel);
        consumerEvent.ReceivedAsync += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            messageReceived.SetResult(true);
            messageEventReceived.SetResult(message);
            //messageEventReceived.SetResult(JsonSerializer.Deserialize<T>(message));
            await Channel.BasicAckAsync(ea.DeliveryTag, false);
        };

        // Act
        await Channel.BasicConsumeAsync(queue: queueName,
            autoAck: false,
            consumer: consumerEvent);
    }
}

public class Producer<T> : IProducer<T> where T : class
{
    private readonly IChannel Channel;

    public Producer(IChannel channel)
    {
        Channel = channel;
    }

    public async Task Send(DomainEvent<T> message)
    {
        var messageJson = JsonSerializer.Serialize(message);
        var messageBody = Encoding.UTF8.GetBytes(messageJson);
        await Send(message.EventType, messageBody);
    }

    public async Task Send(string queueName, byte[] message)
    {
        var properties = new BasicProperties
        {
            Persistent = true
        };
        await Channel.BasicPublishAsync(exchange: string.Empty,
            routingKey: queueName,
            mandatory: true,
            basicProperties: properties,
            body: message);
    }
}

public class UnitOfWork : IUnitOfWork
{
    private readonly TestDbContext _context;
    private IDbContextTransaction _dbContextTransaction;

    public UnitOfWork(TestDbContext context)
    {
        _context = context;
    }

    public async Task BeginTransactionAsync()
    {
        _dbContextTransaction = await _context.Database.BeginTransactionAsync();
    }

    public async Task CommitAsync()
    {
        await _dbContextTransaction.CommitAsync();
    }

    public async Task RollbackAsync()
    {
        await _dbContextTransaction.RollbackAsync();
    }

    public ICustomerRepository Customers
        => new CustomerRepository(_context);

    public IOrderRepository Orders
        => new OrderRepository(_context);
}

public class CustomerRepository : ICustomerRepository
{
    private readonly TestDbContext _context;

    public CustomerRepository(TestDbContext context)
    {
        _context = context;
    }

    public async Task AddCustomerAsync(Customer customer)
    {
        await _context.Set<Customer>().AddAsync(customer);
        await _context.SaveChangesAsync();
    }

    public async Task<Customer?> GetCustomerAsync(Customer customer) => await _context
        .Set<Customer>()
        .FirstOrDefaultAsync(c => c.Email == customer.Email);
}

public class OrderRepository : IOrderRepository
{
    private readonly TestDbContext _context;

    public OrderRepository(TestDbContext context)
    {
        _context = context;
    }

    public async Task AddOrderAsync(Order order)
    {
        await _context.Set<Order>().AddAsync(order);
        await _context.SaveChangesAsync();
    }

    public async Task<Order?> GetOrderAsync(Order order) => await _context
        .Set<Order>()
        .Include(o => o.Customer)
        .FirstOrDefaultAsync(o => o.Id == order.Id);
}