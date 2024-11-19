using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Presentation;
using Presentation.Commons;
using Presentation.Controllers;
using RabbitMQ.Client;
using Testcontainers.MsSql;
using Testcontainers.RabbitMq;

namespace IntegrationTests;

public class IntegrationTestWebAppFactory
    : WebApplicationFactory<Program>, IDisposable
{
    private readonly RabbitMqContainer _rabbitMqContainer = new RabbitMqBuilder()
        .WithImage("rabbitmq:3-alpine")
        .Build();

    private readonly MsSqlContainer _dbContainer = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .WithPassword("Strong_password_123!")
        .Build();
    public IntegrationTestWebAppFactory()
    {
        _rabbitMqContainer.StartAsync().Wait();
        _dbContainer.StartAsync().Wait();
    }

    public void Dispose()
    {
        _rabbitMqContainer.StopAsync().Wait();
        _dbContainer.StopAsync().Wait();
        
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<ConnectionFactory>();
            services.AddSingleton(sp =>
            {
                var uri = new Uri(_rabbitMqContainer.GetConnectionString());
                return new ConnectionFactory()
                {
                    Uri = uri,
                };
            });
            var connectionString = new SqlConnectionStringBuilder(_dbContainer.GetConnectionString());
            connectionString.InitialCatalog = Guid.NewGuid().ToString("D");
        
            var serviceProvider = new ServiceCollection()
                .AddEntityFrameworkSqlServer()
                .BuildServiceProvider();
        
            var builder = new DbContextOptionsBuilder<MyContext>();
            var options = builder
                .UseSqlServer(connectionString.ToString())
                .UseInternalServiceProvider(serviceProvider)
                .Options;
        
            MyContext dbContext = new MyContext(options);
            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();
            dbContext.Database.Migrate();
            services.AddScoped<IUnitOfWork, UnitOfWork>(x=> new UnitOfWork(dbContext));
        });
    }
}

public class HttpClientIntegrationTests
{
    private readonly IntegrationTestWebAppFactory _factory;
    private readonly HttpClient _client;
    private readonly IUnitOfWork _dbContext;

    public HttpClientIntegrationTests()
    {
        
        _factory = new IntegrationTestWebAppFactory();
        _client = _factory.CreateDefaultClient();
        var scope = _factory.Services.CreateScope();
        _dbContext = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
    }
    [Fact]
    public async Task Get_ListarTodos_RetornaSucesso()
    {
        var response = await _client.GetAsync(Constantes.GET_URI_PATH);
        // Assert
        response.Should().BeSuccessful();
        var users = await response.Content.ReadFromJsonAsync<WeatherForecast[]>();
        users.Should().NotBeNull();
        users.Should().NotBeEmpty();
    }
    
    [Fact]
    public async Task Get_ListarTodos_RetornaUsuarioPersistido()
    {
        var response = await _client.GetAsync(Constantes.GET_URI_PATH);
        // Assert
         var usuario = await _dbContext.Users.FindAllAsync(x=>x.Id != 0);
         usuario[0].Id.Should().Be(1);
    }
    
    [Fact]
    public async Task Get_ListarTodos_RetornaUsuarioConsumido()
    {
        var response = await _client.GetAsync(Constantes.GET_URI_PATH);
        // Assert
        var usuario = await _dbContext.Users.FindAllAsync(x=>x.Id != 0);
        usuario[0].Id.Should().Be(1);
    }
}