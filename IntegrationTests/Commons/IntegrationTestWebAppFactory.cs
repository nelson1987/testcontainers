using System.Data.SqlClient;
using Application;
using Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Presentation;
using Presentation.Commons;
using RabbitMQ.Client;

namespace IntegrationTests;

public class IntegrationTestWebAppFactory
    : WebApplicationFactory<Program>, IDisposable
{
    public IChannel _channel { get; private set; }
    public DbContextOptions<TestDbContext> _options { get; private set; }

    public void SetChannel(IConnection rabbitConnection)
    {
        _channel = rabbitConnection.CreateChannelAsync().GetAwaiter().GetResult();
    }

    public void SetDbContextOptions(string SqlConnectionString)
    {
        _options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlServer(SqlConnectionString)
            .ConfigureWarnings(w => w.Throw(RelationalEventId.MultipleCollectionIncludeWarning))
            .Options;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IChannel>();
            services.RemoveAll<TestDbContext>();
            
            services.AddApplication()
                .AddInfrastructure();

            services.AddSingleton<IChannel>(_ => _channel);
            
            var testDbContext = new TestDbContext(_options);
            testDbContext.Database.EnsureCreatedAsync().GetAwaiter().GetResult();
            testDbContext.Database.MigrateAsync().GetAwaiter().GetResult();
            
            services.AddSingleton<TestDbContext>(_ => testDbContext);
            //var connectionString = Guid.NewGuid().ToString("D");
            //services.AddDbContext<TestDbContext>(_ => _.UseSqlServer(SqlConnectionString));
        });
    }
}