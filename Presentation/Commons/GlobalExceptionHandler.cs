using System.Net;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Commons;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Uma exceção ocorreu: {Message}", exception.Message);

        var problemDetails = new ProblemDetails
        {
            Status = GetStatusCode(exception),
            Title = GetTitle(exception),
            Detail = exception.Message,
            Type = exception.GetType().Name
        };

        httpContext.Response.StatusCode = problemDetails.Status.Value;
        
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        
        return true;
    }

    private static int GetStatusCode(Exception exception) => exception switch
    {
        ArgumentNullException => (int)HttpStatusCode.BadRequest,
        ArgumentException => (int)HttpStatusCode.BadRequest,
        InvalidOperationException => (int)HttpStatusCode.Conflict,
        UnauthorizedAccessException => (int)HttpStatusCode.Unauthorized,
        KeyNotFoundException => (int)HttpStatusCode.NotFound,
        _ => (int)HttpStatusCode.InternalServerError
    };

    private static string GetTitle(Exception exception) => exception switch
    {
        ArgumentNullException => "Valor nulo fornecido",
        ArgumentException => "Argumento inválido",
        InvalidOperationException => "Operação inválida",
        UnauthorizedAccessException => "Acesso não autorizado",
        KeyNotFoundException => "Recurso não encontrado",
        _ => "Erro interno do servidor"
    };
}