using System.Data.SqlClient;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using Testcontainers.MsSql;
using Testcontainers.RabbitMq;

namespace IntegrationTests;

public class SharedTestInfrastructure : IAsyncLifetime
{
    private readonly MsSqlContainer _sqlContainer;

    private readonly RabbitMqContainer _rabbitContainer;

    // private readonly IServiceProvider _serviceProvider;
    // private readonly HttpClient _httpClient;
    public string SqlConnectionString { get; private set; }

    public IConnection RabbitConnection { get; private set; }

    //public IHttpClientFactory HttpClientFactory { get; private set; }
    public HttpClient Client { get; private set; }

    public SharedTestInfrastructure()
    {
        _sqlContainer = new MsSqlBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
            .WithPassword("Strong_password_123!")
            .Build();

        _rabbitContainer = new RabbitMqBuilder()
            .WithImage("rabbitmq:3-management")
            .WithUsername("guest")
            .WithPassword("guest")
            .Build();
        
        // Configuração do HttpClient com DI
        //var services = new ServiceCollection();
        // services.AddHttpClient("TestClient", client =>
        // {
        //     client.BaseAddress = new Uri("https://api.example.com/");
        //     client.DefaultRequestHeaders.Add("Accept", "application/json");
        // });
        //
        // _serviceProvider = services.BuildServiceProvider();
        // HttpClientFactory = _serviceProvider.GetRequiredService<IHttpClientFactory>();
    }

    public async Task InitializeAsync()
    {
        // Inicia os containers em paralelo
        await Task.WhenAll(
            _sqlContainer.StartAsync(),
            _rabbitContainer.StartAsync()
        );

        SqlConnectionString = _sqlContainer.GetConnectionString();

        // Configura a conexão do RabbitMQ
        var connectionFactory = new ConnectionFactory();
        connectionFactory.Uri = new Uri(_rabbitContainer.GetConnectionString());
        RabbitConnection = await connectionFactory.CreateConnectionAsync();
      
        var factory = new IntegrationTestWebAppFactory();
        factory.SetChannel(RabbitConnection);
        factory.SetDbContextOptions(SqlConnectionString);
        Client = factory.CreateDefaultClient();
    }

    public async Task DisposeAsync()
    {
        RabbitConnection?.Dispose();
        await Task.WhenAll(
            _sqlContainer.DisposeAsync().AsTask(),
            _rabbitContainer.DisposeAsync().AsTask()
        );
        // if (_serviceProvider is IDisposable disposable)
        //     disposable.Dispose();
    }
}