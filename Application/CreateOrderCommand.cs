namespace Application;

public record CreateOrderCommand(string CustomerName, string CustomerEmail, int CustomerAge, decimal OrderTotal);