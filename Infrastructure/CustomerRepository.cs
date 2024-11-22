using Domain;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure;

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