using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Commons;
using Testcontainers.MsSql;

namespace IntegrationTests;

public class EFIntegrationTests
{
    private readonly MsSqlContainer _dbContainer = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .WithPassword("Strong_password_123!")
        .Build();
    [Fact]
    public async Task AddAsync_DadosValidos_CriarUsuarioComId()
    {
        await _dbContainer.StartAsync();
        var connectionString = new SqlConnectionStringBuilder(_dbContainer.GetConnectionString());
        connectionString.InitialCatalog = Guid.NewGuid().ToString("D");
        
        var serviceProvider = new ServiceCollection()
            .AddEntityFrameworkSqlServer()
            .BuildServiceProvider();
        
        var builder = new DbContextOptionsBuilder<MyContext>();
        var options = builder
            .UseSqlServer(connectionString.ToString())
            .UseInternalServiceProvider(serviceProvider)
            .Options;
        
        MyContext dbContext = new MyContext(options);
        dbContext.Database.EnsureDeleted();
        dbContext.Database.EnsureCreated();
        dbContext.Database.Migrate();
        
        var user = new User(0, "LUCIANO PEREIRA", 33, true);

        // REPOSITORY
        await dbContext.User.AddAsync(user);
        await dbContext.SaveChangesAsync();
        
        await _dbContainer.StopAsync();

        // ASSERT
        user.Id.Should().Be(1);
    }
}