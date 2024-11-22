using Application;
using Microsoft.AspNetCore.Mvc;
// using Microsoft.EntityFrameworkCore;
// using Microsoft.EntityFrameworkCore.Storage;
// using Presentation.Commons;

namespace Presentation.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class ContratosController : ControllerBase
{
    // private static readonly string[] Summaries = new[]
    // {
    //     "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    // };
    private readonly ICreateOrderHandler _handler;

    public ContratosController(ICreateOrderHandler handler)
    {
        _handler = handler;
    }

    /*
    private readonly ILogger<ContratosController> _logger;
    private readonly IMessageProducer<CreatedUserEvent> _createUserProducer;
    private readonly IUserDomainService _userRepository;

    public ContratosController(ILogger<ContratosController> logger,
        IMessageProducer<CreatedUserEvent> createUserProducer,
        IUserDomainService userRepository)
    {
        _logger = logger;
        _createUserProducer = createUserProducer;
        _userRepository = userRepository;
    }
*/
    [HttpPost]
    public async Task<IActionResult> Post(CancellationToken cancellationToken)
    {
        // await _createUserProducer.SendMessage(new CreatedUserEvent(5, "John Doe", 18, true));
        // await _userRepository.CreateUserAsync(new User(0, "John Doe", 18, true));
        var command = new CreateOrderCommand("John Doe", "johndoe@email.com",18,100.00M);
        await _handler.Handle(command, cancellationToken);
        return Ok();
    }
}