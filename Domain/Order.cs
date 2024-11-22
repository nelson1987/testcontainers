namespace Domain;

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