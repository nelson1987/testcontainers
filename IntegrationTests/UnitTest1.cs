using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MsSql;

namespace IntegrationTests;
public class MyContext: DbContext
{
    public DbSet<User> User { get; set; }
    public MyContext(DbContextOptions<MyContext> options) : base(options)
    {

    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<User>(e =>
        {
            e
                .ToTable("TB_USER")
                .HasKey(k => k.Id);

            e
                .Property(p => p.Id)
                .ValueGeneratedOnAdd();
        });
    }
}
public class User
{
    public User()
    {

    }
    public User(int Id, string Name, int Age, bool IsActive)
    {
        this.Id = Id;
        this.Name = Name;
        this.Age = Age;
        this.IsActive = IsActive;
    }
    public int Id { get; set; }
    public string Name { get; set; }
    public int Age { get; set; }
    public bool IsActive { get; set; }
}
public class UnitTest1
{
    private readonly MsSqlContainer _dbContainer = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .WithPassword("Strong_password_123!")
        .Build();
    [Fact]
    public async Task Test1()
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