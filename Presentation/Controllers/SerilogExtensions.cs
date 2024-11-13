using Serilog;

namespace Presentation.Controllers;

public static class SerilogExtensions
{
    public static void AddSerilog(this ConfigureHostBuilder host)
    {
        host.UseSerilog((context, services, configuration) => configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.File("logs/myapp-.txt", 
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7)
        );
    }
}