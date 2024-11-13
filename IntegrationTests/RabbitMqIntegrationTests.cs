using FluentAssertions;
using RabbitMQ.Client;
using Testcontainers.RabbitMq;

namespace IntegrationTests;
public class RabbitMqIntegrationTests
{
    private readonly RabbitMqContainer _rabbitMqContainer = new RabbitMqBuilder()
        .WithImage("rabbitmq:3.11")
        .Build();
    
    [Fact]
    public async Task AddAsync_DadosValidos_CriarUsuarioComId()
    {
        await _rabbitMqContainer.StartAsync();
        
        // Given
        var connectionFactory = new ConnectionFactory();
        connectionFactory.Uri = new Uri(_rabbitMqContainer.GetConnectionString());

        // When
        using var connection = connectionFactory.CreateConnection();

        // Then
        connection.IsOpen.Should().BeTrue();
        
        
        // var connectionString = new SqlConnectionStringBuilder(_dbContainer.GetConnectionString());
        // connectionString.InitialCatalog = Guid.NewGuid().ToString("D");
        //
        // var serviceProvider = new ServiceCollection()
        //     .AddEntityFrameworkSqlServer()
        //     .BuildServiceProvider();
        //
        // var builder = new DbContextOptionsBuilder<MyContext>();
        // var options = builder
        //     .UseSqlServer(connectionString.ToString())
        //     .UseInternalServiceProvider(serviceProvider)
        //     .Options;
        //
        // MyContext dbContext = new MyContext(options);
        // dbContext.Database.EnsureDeleted();
        // dbContext.Database.EnsureCreated();
        // dbContext.Database.Migrate();
        //
        // var user = new User(0, "LUCIANO PEREIRA", 33, true);
        //
        // // REPOSITORY
        // await dbContext.User.AddAsync(user);
        // await dbContext.SaveChangesAsync();
        
        await _rabbitMqContainer.StopAsync();

        // ASSERT
        //user.Id.Should().Be(1);
    }
}