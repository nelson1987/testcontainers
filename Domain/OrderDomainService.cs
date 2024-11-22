namespace Domain;

public class OrderDomainService : IOrderDomainService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IProducer<CreatedOrderEvent> _createdOrderProducer;

    public OrderDomainService(IUnitOfWork unitOfWork, IProducer<CreatedOrderEvent> createdOrderProducer)
    {
        _unitOfWork = unitOfWork;
        _createdOrderProducer = createdOrderProducer;
    }

    public async Task AddOrderAsync(Order order)
    {
        try
        {
            await _unitOfWork.Customers.AddCustomerAsync(order.Customer);
            await _unitOfWork.Orders.AddOrderAsync(order);
            await _unitOfWork.CommitAsync();
            await _createdOrderProducer.Send(order.ToCreatedEvent());
        }
        catch (Exception)
        {
            await _unitOfWork.RollbackAsync();
            throw;
        }
    }

    public async Task<Order?> FindOrderAsync(Order order)
        => await _unitOfWork.Orders.GetOrderAsync(order);
}