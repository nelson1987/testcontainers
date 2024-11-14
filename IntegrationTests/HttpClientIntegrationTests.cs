using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Presentation;
using Presentation.Commons;
using RabbitMQ.Client;
using Testcontainers.MsSql;
using Testcontainers.RabbitMq;

namespace IntegrationTests;

// public interface IRabbitMqConnection
// {
//     Task InitializeBroker();
// }
//
// public class RabbitMqConnection : IRabbitMqConnection, IAsyncLifetime
// {
//     private readonly RabbitMqContainer _rabbitMqContainer = new RabbitMqBuilder()
//         .WithImage("rabbitmq:3.11")
//         .Build();
//     
//     private IChannel _channel;
//     public IChannel Channel => _channel!;
//
//     public async Task InitializeAsync()
//     {
//         await _rabbitMqContainer.StartAsync();
//     }
//     
//     public async Task InitializeBroker()
//     {
//         var connectionFactory = new ConnectionFactory
//         {
//             Uri = new Uri(_rabbitMqContainer.GetConnectionString())
//         };
//         var connection = await connectionFactory.CreateConnectionAsync();
//         _channel = await connection.CreateChannelAsync();
//     }
//
//     public async Task DisposeAsync()
//     {
//         await _rabbitMqContainer.StopAsync();
//     }
// }
//
 
public class IntegrationTestWebAppFactory
    : WebApplicationFactory<Program>//,
        //IAsyncLifetime
{
    // private readonly MsSqlContainer _msSqlContainer = new MsSqlBuilder()
    //     .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
    //     .WithPassword("Strong_password_123!")
    //     .Build();


     private readonly RabbitMqContainer _rabbitMqContainer = new RabbitMqBuilder()
         .WithImage("rabbitmq:3.11")
         .Build();

     public IntegrationTestWebAppFactory()
     {
         _rabbitMqContainer.StartAsync();
     }
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            //var @event = new CreatedUserEvent(6, "John Doe", 18, false);

            //using var connection = await connectionFactory.CreateConnectionAsync();
            //using var _channel = await connection.CreateChannelAsync();

            services.AddSingleton(sp =>
            {
                var uri = new Uri(_rabbitMqContainer.GetConnectionString());
                return new ConnectionFactory()
                {
                    Uri = uri
                };
            });

            //     var descriptorType =
            //         typeof(DbContextOptions<ApplicationDbContext>);
            //
            //     var descriptor = Enumerable
            //         .SingleOrDefault<ServiceDescriptor>(services, s => s.ServiceType == descriptorType);
            //
            //     if (descriptor is not null) services.Remove(descriptor);
            //
            //     EntityFrameworkServiceCollectionExtensions.AddDbContext<ApplicationDbContext>(services, options =>
            //         SqlServerDbContextOptionsExtensions.UseSqlServer(options, _dbContainer.GetConnectionString()));
        });
    }

    // public async Task InitializeAsync()
    // {
    //     await _msSqlContainer.StartAsync();
    // }
    //
    // public async Task DisposeAsync()
    // {
    //     await _msSqlContainer.StopAsync();
    // }
}
public class HttpClientIntegrationTests
{
    [Fact]
    public async Task Get_ListarTodos_RetornaSucesso()
    {
        var factory = new IntegrationTestWebAppFactory();
        var client = factory.CreateDefaultClient();
        var response = await client.GetAsync(Constantes.GET_URI_PATH);
        // Assert
        response.Should().BeSuccessful();
        var users = await response.Content.ReadFromJsonAsync<WeatherForecast[]>();
        users.Should().NotBeNull();
        users.Should().NotBeEmpty();
    }
}