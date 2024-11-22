using Domain;
using Microsoft.EntityFrameworkCore.Storage;

namespace Infrastructure;

public class UnitOfWork : IUnitOfWork
{
    private readonly TestDbContext _context;
    private IDbContextTransaction _dbContextTransaction;

    public UnitOfWork(TestDbContext context)
    {
        _context = context;
        _dbContextTransaction = _context.Database.BeginTransaction();
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