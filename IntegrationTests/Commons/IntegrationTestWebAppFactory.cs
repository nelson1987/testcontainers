using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Presentation;

namespace IntegrationTests;

public class IntegrationTestWebAppFactory
    : WebApplicationFactory<Program>, IDisposable
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // services.RemoveAll<ConnectionFactory>();
            // services.AddSingleton(sp =>
            // {
            //     var uri = new Uri(_rabbitMqContainer.GetConnectionString());
            //     return new ConnectionFactory()
            //     {
            //         Uri = uri,
            //     };
            // });
            // var connectionString = new SqlConnectionStringBuilder(_dbContainer.GetConnectionString());
            // connectionString.InitialCatalog = Guid.NewGuid().ToString("D");
            //
            // var serviceProvider = new ServiceCollection()
            //     .AddEntityFrameworkSqlServer()
            //     .BuildServiceProvider();
            //
            // var builder = new DbContextOptionsBuilder<MyContext>();
            // var options = builder
            //     .UseSqlServer(connectionString.ToString())
            //     .UseInternalServiceProvider(serviceProvider)
            //     .Options;
            //
            // MyContext dbContext = new MyContext(options);
            // dbContext.Database.EnsureDeleted();
            // dbContext.Database.EnsureCreated();
            // dbContext.Database.Migrate();
            // services.AddScoped<IUnitOfWork, UnitOfWork>(x=> new UnitOfWork(dbContext));
        });
    }
}