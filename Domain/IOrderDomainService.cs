namespace Domain;

public interface IOrderDomainService
{
    Task AddOrderAsync(Order order);
    Task<Order?> FindOrderAsync(Order order);
}