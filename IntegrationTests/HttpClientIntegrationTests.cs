using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Presentation;
using Presentation.Commons;
using RabbitMQ.Client;
using Testcontainers.MsSql;
using Testcontainers.RabbitMq;

namespace IntegrationTests;

public class IntegrationTestWebAppFactory
    : WebApplicationFactory<Program>
{
    private readonly RabbitMqContainer _rabbitMqContainer = new RabbitMqBuilder()
        .WithImage("rabbitmq:3-alpine")
        .Build();

    public IntegrationTestWebAppFactory()
    {
        _rabbitMqContainer.StartAsync().Wait();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        //_rabbitMqContainer.StartAsync().GetAwaiter();
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
        });
    }
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