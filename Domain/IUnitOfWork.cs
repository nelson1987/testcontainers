namespace Domain;

public interface IUnitOfWork
{
    Task CommitAsync();
    Task RollbackAsync();
    ICustomerRepository Customers { get; }
    IOrderRepository Orders { get; }
}