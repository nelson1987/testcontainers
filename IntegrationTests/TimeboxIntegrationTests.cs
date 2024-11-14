using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MsSql;
using Testcontainers.RabbitMq;
using Xunit.Abstractions;

namespace IntegrationTests;

public class InMemoryDbContextFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _msSqlContainer = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .WithPassword("Strong_password_123!")
        .Build();
    public MyContext Context { get; private set; }

    public InMemoryDbContextFixture()
    {
        Console.WriteLine("1_InMemoryDbContextFixture :: Started");
    }

    public async Task InitializeAsync()
    {
        await _msSqlContainer.StartAsync();
        await InitializeDatabase();
        // _msSqlContainer = 
        // Context = new InMemoryDbContext();
        Console.WriteLine("1_InMemoryDbContextFixture :: InitializeAsync");
        await Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _msSqlContainer.StopAsync();
        await Context.DisposeAsync();
        Console.WriteLine("1_InMemoryDbContextFixture :: DisposeAsync");
        await Task.CompletedTask;
    }
    
    private async Task InitializeDatabase()
    {
        Console.WriteLine("1_InMemoryDbContextFixture :: InitializeDatabase");
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

        Context = new MyContext(options);
        await Context.Database.EnsureDeletedAsync();
        await Context.Database.EnsureCreatedAsync();
        await Context.Database.MigrateAsync();
    }
}




// public class User
// {
// }
//
// public class InMemoryDbContext : IAsyncLifetime
// {
//     public List<User> Users { get; internal set; }
//
//     public async Task InitializeAsync()
//     {
//         Console.WriteLine("InMemoryDbContext :: InitializeAsync");
//         await Task.CompletedTask;
//     }
//
//     public async Task DisposeAsync()
//     {
//         Console.WriteLine("InMemoryDbContext :: DisposeAsync");
//         await Task.CompletedTask;
//     }
// }


[CollectionDefinition("Context collection")]
public class InMemoryDbContextFixtureCollection : ICollectionFixture<InMemoryDbContextFixture>
{
}

[Collection("Context collection")]
public class ContextTestClass1 : IAsyncLifetime
{
    protected MyContext inMemoryDbContext;

    public ContextTestClass1(InMemoryDbContextFixture fixture)
    {
        Console.WriteLine("2_ContextTestClass :: Started");
        inMemoryDbContext = fixture.Context;
    }

    public async Task InitializeAsync()
    {
        Console.WriteLine("2_ContextTestClass :: InitializeAsync");
        await Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        Console.WriteLine("ContextTestClass1 :: DisposeAsync");
        await Task.CompletedTask;
    }
}

public class InMemoryDbContextTests : ContextTestClass1
{
    public InMemoryDbContextTests(InMemoryDbContextFixture fixture) : base(fixture)
    {
        Console.WriteLine("3_InMemoryDbContextTests :: Started");
    }

    [Fact]
    public void WithNoItems_CountShouldReturnZero()
    {
        Console.WriteLine("InMemoryDbContextTests :: WithNoItems_CountShouldReturnZero");
        var count = inMemoryDbContext.User.Count();
        
        Assert.Equal(0, count);
    }

    [Fact]
    public void AfterAddingItem_CountShouldReturnOne()
    {
        Console.WriteLine("InMemoryDbContextTests :: AfterAddingItem_CountShouldReturnOne");
        var user = new User(0, "LUCIANO PEREIRA", 33, true);
        inMemoryDbContext.User.Add(user);
        inMemoryDbContext.SaveChanges();
        
        var count = inMemoryDbContext.User.Count();
        
        Assert.Equal(1, count);
    }
}


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
/*
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
        // await _msSqlContainer.StartAsync();
        // await InitializeDatabase();
        await Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        Console.WriteLine("DatabaseFixture :: DisposeAsync");
        // await _msSqlContainer.StopAsync();
        await Task.CompletedTask;
    }
}

[CollectionDefinition("Context collection")]
public class InMemoryDbContextFixtureCollection : ICollectionFixture<DatabaseFixture>
{
}

[Collection("Context collection")]
public class IntegrationTestsBase : IAsyncLifetime
{
    // private readonly RabbitMqContainer _rabbitMqContainer = new RabbitMqBuilder()
    //     .WithImage("rabbitmq:3.11")
    //     .Build();
    //
    // protected MyContext? DbContext { get; }

    protected IntegrationTestsBase(DatabaseFixture fixture)
    {
        Console.WriteLine("IntegrationTestsBase :: IntegrationTestsBase");
        // DbContext = fixture.DbContext;
    }

    public async Task InitializeAsync()
    {
        Console.WriteLine("IntegrationTestsBase :: InitializeAsync");
        // await _rabbitMqContainer.StartAsync();
        await Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        Console.WriteLine("IntegrationTestsBase :: DisposeAsync");
        // await _rabbitMqContainer.StopAsync();
        await Task.CompletedTask;
    }
}

public class TimeboxIntegrationTests : IntegrationTestsBase
{
    public TimeboxIntegrationTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task Get_ListarTodos_RetornaSucesso()
    {
        Console.WriteLine("TimeboxIntegrationTests :: Get_ListarTodos_RetornaSucesso");
        await Task.CompletedTask;
        // var user = new User(0, "LUCIANO PEREIRA", 33, true);
        //
        // // REPOSITORY
        // await DbContext!.User.AddAsync(user);
        // await DbContext.SaveChangesAsync();
        //
        // // ASSERT
        // user.Id.Should().Be(1);
    }
}


public class Timebox_v2IntegrationTests : IntegrationTestsBase
{
    public Timebox_v2IntegrationTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task Get_ListarTodos_RetornaSucesso()
    {
        Console.WriteLine("TimeboxIntegrationTests :: Get_ListarTodos_RetornaSucesso");
        await Task.CompletedTask;
        // var user = new User(0, "LUCIANO PEREIRA", 33, true);
        //
        // // REPOSITORY
        // await DbContext!.User.AddAsync(user);
        // await DbContext.SaveChangesAsync();
        //
        // // ASSERT
        // user.Id.Should().Be(1);
    }
}
*/