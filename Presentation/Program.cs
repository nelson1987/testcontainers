using Application;
using Infrastructure;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Presentation.Commons;
using Presentation.Controllers;
using RabbitMQ.Client;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
builder.Host.AddSerilog();
builder.Services.AddApplication();
builder.Services.AddInfrastructure();
// builder.Services.AddScoped(typeof(IBaseRepository<>), typeof(BaseRepository<>));
// builder.Services.AddScoped<IUserDomainService, UserDomainService>();
// builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
// builder.Services.AddScoped<IUserRepository, UserRepository>();
// var connectionString = new SqlConnectionStringBuilder("");
// connectionString.InitialCatalog = Guid.NewGuid().ToString("D");
// builder.Services.AddDbContext<MyContext>(options => 
//     options.UseSqlServer(connectionString.ToString()));
// builder.Services.AddScoped<IMessageProducer<CreatedUserEvent>, CheckoutItemProducer>();
// builder.Services.AddSingleton(sp =>
// {
//     var uri = new Uri("URL FOR RABBITMQ SERVER");
//     return new ConnectionFactory
//     {
//         Uri = uri
//     };
// });
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
app.UseRequestContextLogging();
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