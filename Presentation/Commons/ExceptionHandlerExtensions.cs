namespace Presentation.Commons;

public static class ExceptionHandlerExtensions
{
    public static void AddGlobalExceptionHandler(this IServiceCollection services)
    {
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();
    }
}