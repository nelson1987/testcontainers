namespace Domain;

public class Customer
{
    protected Customer()
    {
    }

    public Customer(int id, string name, string email, int age)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(id, 0);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentOutOfRangeException.ThrowIfLessThan(age, 18);
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
    protected Order()
    {
    }

    public Order(int id, DateTime orderDate, decimal total, Customer customer)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(id, 0);
        ArgumentOutOfRangeException.ThrowIfLessThan(orderDate, DateTime.UtcNow.AddSeconds(-5));
        ArgumentOutOfRangeException.ThrowIfLessThan(total, 0.01M);
        ArgumentNullException.ThrowIfNull(customer);
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

public class DomainEvent<T>
    where T : class
{
    public string EventType = typeof(T).FullName;
    public string EventId = Guid.NewGuid().ToString("D");
    public DateTime EventDate = DateTime.UtcNow;

    public DomainEvent(T message)
    {
        Message = message;
    }

    public T Message { get; private set; }
}

public record CreatedCustomerEvent(int CustomerId);

public record CreatedOrderEvent(int OrderId);

public interface IOrderDomainService
{
    Task AddOrderAsync(Order order);
    Task<Order?> FindOrderAsync(Order order);
}

public static class CreatedOrderEventExtensions
{
    public static DomainEvent<CreatedOrderEvent> ToCreatedEvent(this Order order)
        => new DomainEvent<CreatedOrderEvent>(new CreatedOrderEvent(order.Id));
}

public class OrderDomainService : IOrderDomainService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IProducer<CreatedOrderEvent> _createdOrderproducer;

    public OrderDomainService(IUnitOfWork unitOfWork, IProducer<CreatedOrderEvent> createdOrderproducer)
    {
        _unitOfWork = unitOfWork;
        _createdOrderproducer = createdOrderproducer;
    }

    public async Task AddOrderAsync(Order order)
    {
        try
        {
            await _unitOfWork.BeginTransactionAsync();
            await _unitOfWork.Customers.AddCustomerAsync(order.Customer);
            await _unitOfWork.Orders.AddOrderAsync(order);
            await _unitOfWork.CommitAsync();
            await _createdOrderproducer.Send(order.ToCreatedEvent());
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackAsync();
        }
    }

    public async Task<Order?> FindOrderAsync(Order order)
        => await _unitOfWork.Orders.GetOrderAsync(order);
}

public interface IUnitOfWork
{
    Task BeginTransactionAsync();
    Task CommitAsync();
    Task RollbackAsync();
    ICustomerRepository Customers { get; }
    IOrderRepository Orders { get; }
}

public interface ICustomerRepository
{
    Task AddCustomerAsync(Customer customer);
    Task<Customer?> GetCustomerAsync(Customer customer);
}

public interface IOrderRepository
{
    Task AddOrderAsync(Order order);
    Task<Order?> GetOrderAsync(Order order);
}

public interface IProducer<T> where T : class
{
    Task Send(DomainEvent<T> message);
}

public interface IConsumer<T> where T : class
{
    TaskCompletionSource<bool> messageReceived { get; }
    TaskCompletionSource<string> messageEventReceived { get; }
    Task Consume(string queueName);
    Task Consume();
}