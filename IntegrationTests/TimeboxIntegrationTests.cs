using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MsSql;
using Testcontainers.RabbitMq;
using Xunit.Abstractions;

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
public class DatabaseFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _msSqlContainer = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .WithPassword("Strong_password_123!")
        .Build();

    public MyContext? DbContext { get; private set; }

    private async Task InitializeDatabase()
    {
        Console.WriteLine("DatabaseFixture :: InitializeDatabase");
        var connectionString = new SqlConnectionStringBuilder(_msSqlContainer.GetConnectionString());
        connectionString.InitialCatalog = Guid.NewGuid().ToString("D");

        var serviceProvider = new ServiceCollection()
            .AddEntityFrameworkSqlServer()
            .BuildServiceProvider();

        var builder = new DbContextOptionsBuilder<MyContext>();
        var options = builder
            .UseSqlServer(connectionString.ToString())
            .UseInternalServiceProvider(serviceProvider)
            .Options;

        DbContext = new MyContext(options);
        await DbContext.Database.EnsureDeletedAsync();
        await DbContext.Database.EnsureCreatedAsync();
        await DbContext.Database.MigrateAsync();
    }

    public async Task InitializeAsync()
    {
        Console.WriteLine("DatabaseFixture :: InitializeAsync");
        await _msSqlContainer.StartAsync();
        await InitializeDatabase();
    }

    public async Task DisposeAsync()
    {
        Console.WriteLine("DatabaseFixture :: DisposeAsync");
        await _msSqlContainer.StopAsync();
    }
}

[Collection("IntegrationTests")]
public class IntegrationTestsBase : IAsyncLifetime
{
    private readonly RabbitMqContainer _rabbitMqContainer = new RabbitMqBuilder()
        .WithImage("rabbitmq:3.11")
        .Build();

    protected MyContext? DbContext { get; }

    protected IntegrationTestsBase(DatabaseFixture fixture)
    {
        Console.WriteLine("IntegrationTestsBase :: IntegrationTestsBase");
        DbContext = fixture.DbContext;
    }

    public async Task InitializeAsync()
    {
        Console.WriteLine("IntegrationTestsBase :: InitializeAsync");
        await _rabbitMqContainer.StartAsync();
    }

    public async Task DisposeAsync()
    {
        Console.WriteLine("IntegrationTestsBase :: DisposeAsync");
        await _rabbitMqContainer.StopAsync();
    }
}

public class TimeboxIntegrationTests : IntegrationTestsBase
{
    private readonly ITestOutputHelper _testOutputHelper;

    protected TimeboxIntegrationTests(DatabaseFixture fixture, ITestOutputHelper testOutputHelper) : base(fixture)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Fact]
    public async Task Get_ListarTodos_RetornaSucesso()
    {
        _testOutputHelper.WriteLine("TimeboxIntegrationTests :: Get_ListarTodos_RetornaSucesso");
        var user = new User(0, "LUCIANO PEREIRA", 33, true);
        
        // REPOSITORY
        await DbContext!.User.AddAsync(user);
        await DbContext.SaveChangesAsync();
        
        // ASSERT
        user.Id.Should().Be(1);
        /*
IntegrationTestsBase
TimeboxIntegrationTests
InitializeAsync
Get_ListarTodos_RetornaSucesso
DisposeAsync
         */
    }
}