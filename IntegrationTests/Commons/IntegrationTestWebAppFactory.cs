using System.Data.SqlClient;
using Application;
using Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Presentation;
using Presentation.Commons;
using RabbitMQ.Client;

namespace IntegrationTests;

public class IntegrationTestWebAppFactory
    : WebApplicationFactory<Program>, IDisposable
{
    public Uri rabbitMqConnectionString { get; private set; }
    public SqlConnectionStringBuilder msSqlConnectionString { get; private set; }

    public void SetRabbitMqConnectionString(Uri uri)
    {
        rabbitMqConnectionString = uri;
    }

    public void SetMsSqlConnectionString(SqlConnectionStringBuilder connectionString)
    {
        msSqlConnectionString = connectionString;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // services.RemoveAll<ConnectionFactory>();
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
        });
    }
}