using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Presentation;
using Presentation.Commons;

namespace IntegrationTests;

public class HttpClientIntegrationTests
{
    [Fact]
    public async Task Get_ListarTodos_RetornaSucesso()
    {
        var factory = new WebApplicationFactory<Program>();
        var client = factory.CreateDefaultClient();
        var response = await client.GetAsync(Constantes.GET_URI_PATH);
        // Assert
        response.Should().BeSuccessful();
        var users = await response.Content.ReadFromJsonAsync<WeatherForecast[]>();
        users.Should().NotBeNull();
        users.Should().NotBeEmpty();
    }
}