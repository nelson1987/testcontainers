using Microsoft.AspNetCore.Mvc;
using Presentation.Commons;

namespace Presentation.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class ContratosController : ControllerBase
{
    private static readonly string[] Summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    private readonly ILogger<ContratosController> _logger;
    private readonly IMessageProducer<CreatedUserEvent> _checkoutProcessor;

    public ContratosController(ILogger<ContratosController> logger, 
        IMessageProducer<CreatedUserEvent> checkoutProcessor)
    {
        _logger = logger;
        _checkoutProcessor = checkoutProcessor;
    }

    [HttpGet]
    public async Task<IEnumerable<WeatherForecast>> Get()
    {
        await _checkoutProcessor.SendMessage(new CreatedUserEvent(5, "John Doe", 18, true));
        return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();
    }
}