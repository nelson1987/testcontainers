using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Presentation;
using Presentation.Commons;
using Testcontainers.MsSql;
using Testcontainers.RabbitMq;

namespace IntegrationTests;
/*
public interface ISender : IDisposable
{
}

public interface IDbContext : IDisposable
{
}

public abstract class BaseIntegrationTest
    : IClassFixture<IntegrationTestWebAppFactory>,
        IDisposable
{
    private readonly IServiceScope _scope;
    protected readonly HttpClient _client;
    protected readonly ISender _sender;
    protected readonly IDbContext _dbContext;

    protected BaseIntegrationTest(IntegrationTestWebAppFactory factory)
    {
        _scope = factory.Services.CreateScope();
        _sender = _scope.ServiceProvider.GetRequiredService<ISender>();
        _dbContext = _scope.ServiceProvider.GetRequiredService<IDbContext>();
        _client = factory.CreateDefaultClient();
    }

    public void Dispose()
    {
        _scope?.Dispose();
        _sender?.Dispose();
        _dbContext?.Dispose();
    }
}

public class IntegrationTestWebAppFactory
    : WebApplicationFactory<Program>,
        IAsyncLifetime
{
    private readonly MsSqlContainer _msSqlContainer = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .WithPassword("Strong_password_123!")
        .Build();

    private readonly RabbitMqContainer _rabbitMqContainer = new RabbitMqBuilder()
        .WithImage("rabbitmq:3.11")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
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

    public async Task InitializeAsync()
    {
        await _msSqlContainer.StartAsync();
        await _rabbitMqContainer.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _msSqlContainer.StopAsync();
        await _rabbitMqContainer.StopAsync();
    }
}

public class TimeboxIntegrationTests : BaseIntegrationTest
{
    public TimeboxIntegrationTests(IntegrationTestWebAppFactory factory)
        : base(factory)
    {
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
    public async Task Post_DadosValidos_RetornaSucesso()
    {
        var command = new { };
        var response = await _client.PostAsJsonAsync(Constantes.GET_URI_PATH, command);
        // Assert
        response.Should().BeSuccessful();
        var users = await response.Content.ReadFromJsonAsync<WeatherForecast[]>();
        users.Should().NotBeNull();
        users.Should().NotBeEmpty();
    }
}
*/
public class IntegrationTestsBase : IAsyncLifetime
{
    public IntegrationTestsBase()
    {
        Console.WriteLine("IntegrationTestsBase");
    }
    public Task InitializeAsync()
    {
        Console.WriteLine("InitializeAsync");
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        Console.WriteLine("DisposeAsync");
        return Task.CompletedTask;
    }
}

public class TimeboxIntegrationTests : IntegrationTestsBase
{
    public TimeboxIntegrationTests()
    {
        Console.WriteLine("TimeboxIntegrationTests");
    }
    
    [Fact]
    public Task Get_ListarTodos_RetornaSucesso()
    {
        Console.WriteLine("Get_ListarTodos_RetornaSucesso");
        return Task.CompletedTask;
        /*
IntegrationTestsBase
TimeboxIntegrationTests
InitializeAsync
Get_ListarTodos_RetornaSucesso
DisposeAsync
         */
        
    }
}