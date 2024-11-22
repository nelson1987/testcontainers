using System.Data.SqlClient;
using Application;
using Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Presentation;
using Presentation.Commons;
using RabbitMQ.Client;

namespace IntegrationTests;

public class IntegrationTestWebAppFactory
    : WebApplicationFactory<Program>, IDisposable
{
    public IChannel channel { get; private set; }

    public IChannel SetChannel(IConnection rabbitConnection)
    {
        return rabbitConnection.CreateChannelAsync().GetAwaiter().GetResult();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IChannel>();
            // services.AddSingleton(sp => new ConnectionFactory()
            // {
            //     Uri = rabbitMqConnectionString,
            // });
            // var connectionString = msSqlConnectionString;
            // connectionString.InitialCatalog = Guid.NewGuid().ToString("D");
            // services.AddEntityFrameworkSqlServer();
            //     //.BuildServiceProvider();
            //
            // var builder = new DbContextOptionsBuilder<MyContext>();
            // var options = builder
            //     .UseSqlServer(connectionString.ToString())
            //     //.UseInternalServiceProvider(serviceProvider)
            //     .Options;
            //
            // MyContext dbContext = new MyContext(options);
            // dbContext.Database.EnsureDeleted();
            // dbContext.Database.EnsureCreated();
            // dbContext.Database.Migrate();
            // services.AddScoped<IUnitOfWork, UnitOfWork>(x=> new UnitOfWork(dbContext));
            services.AddApplication()
                .AddInfrastructure();

            services.AddSingleton<IChannel>(_ => channel);
        });
    }
}