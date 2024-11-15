using Presentation.Commons;
using RabbitMQ.Client;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
builder.Host.AddSerilog();
builder.Services.AddSingleton<IMessageProducer<CreatedUserEvent>, CheckoutItemProducer>();
builder.Services.AddSingleton(sp =>
{
    var uri = new Uri("URL FOR RABBITMQ SERVER");
    return new ConnectionFactory
    {
        Uri = uri
    };
});
builder.Services.AddGlobalExceptionHandler();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseSerilogRequestLogging();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.UseExceptionHandler();
try
{
    Log.Information("Iniciando aplicação web");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Aplicação terminou inesperadamente");
}
finally
{
    Log.CloseAndFlush();
}

namespace Presentation
{
    public partial class Program
    {
    }
}