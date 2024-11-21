using Domain;
using FluentAssertions;
using FluentAssertions.Extensions;

namespace IntegrationTests;

public class OrderUnitTests
{
    private static readonly Customer customer = new Customer(1, "John", "Doe", 22);

    [Fact]
    public void ConstructingOrder_ShouldCreateCorrectly()
    {
        var order = new Order(0, DateTime.UtcNow, 0.01M, customer);
        order.Id.Should().Be(0);
        order.OrderDate.Should().BeCloseTo(DateTime.UtcNow, 1.Seconds());
        order.Total.Should().Be(0.01M);
        order.Customer.Id.Should().Be(customer.Id);
    }

    [Fact]
    public void ConstructingCustomer_ShouldHaveIdGreaterThanZero()
    {
        var order = () => new Order(-1, DateTime.UtcNow, 0.01M, customer);
        order.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void ConstructingCustomer_ShouldHaveIdOrderDataGreaterThanNow()
    {
        var order = () => new Order(0, DateTime.UtcNow.AddSeconds(-5), 0.01M, customer);
        order.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void ConstructingCustomer_ShouldHaveIdTotalGreaterThanZero()
    {
        var order = () => new Order(0, DateTime.UtcNow, -0.01M, customer);
        order.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void ConstructingCustomer_ShouldHaveRequiredCustomer()
    {
        var order = () => new Order(0, DateTime.UtcNow, 0.01M, null);
        order.Should().Throw<ArgumentNullException>();
    }
}