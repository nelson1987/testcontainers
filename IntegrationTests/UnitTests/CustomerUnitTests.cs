using Domain;
using FluentAssertions;

namespace IntegrationTests;

public class CustomerUnitTests
{
    [Fact]
    public void ConstructingCustomer_ShouldCreateCorrectly()
    {
        var customer = new Customer(1, "John", "Doe", 22);
        customer.Id.Should().Be(1);
        customer.Name.Should().Be("John");
        customer.Email.Should().Be("Doe");
        customer.Age.Should().Be(22);
    }

    [Fact]
    public void ConstructingCustomer_ShouldHaveIdGreaterThanZero()
    {
        var customer = () => new Customer(-1, "John", "Doe", 22);
        customer.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("  ")]
    [InlineData(null)]
    public void ConstructingCustomer_ShouldHaveName(string name)
    {
        var customer = () => new Customer(0, name, "Doe", 22);
        customer.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("  ")]
    [InlineData(null)]
    public void ConstructingCustomer_ShouldHaveEmail(string email)
    {
        var customer = () => new Customer(0, "John", email, 22);
        customer.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(-1)]
    [InlineData(17)]
    public void ConstructingCustomer_ShouldHaveAgeGreaterThan18(int age)
    {
        var customer = () => new Customer(0, "John", "Doe", age);
        customer.Should().Throw<ArgumentOutOfRangeException>();
    }
}