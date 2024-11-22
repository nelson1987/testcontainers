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