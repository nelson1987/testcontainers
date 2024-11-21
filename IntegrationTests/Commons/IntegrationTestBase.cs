using Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace IntegrationTests;

public abstract class IntegrationTestBase
{
    protected readonly TestDbContext DbContext;
    protected readonly IConnection RabbitConnection;
    //protected readonly IHttpClientFactory HttpClientFactory;
    protected readonly SharedTestInfrastructure Infrastructure;
    protected readonly HttpClient Client;

    protected IntegrationTestBase(SharedTestInfrastructure infrastructure)
    {
        Infrastructure = infrastructure;

        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlServer(infrastructure.SqlConnectionString)
            //.LogTo(Console.WriteLine, LogLevel.Information)
            .Options;

        DbContext = new TestDbContext(options);
        RabbitConnection = infrastructure.RabbitConnection;
        //HttpClientFactory = infrastructure.HttpClientFactory;
        Client = infrastructure.Client;
    }
}