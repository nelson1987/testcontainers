namespace Domain;

public interface ICustomerRepository
{
    Task AddCustomerAsync(Customer customer);
    Task<Customer?> GetCustomerAsync(Customer customer);
}