using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using Testcontainers.MsSql;
using Testcontainers.RabbitMq;

namespace IntegrationTests;

public class SharedUnitTests
{
    public class DataBaseFixture : IAsyncLifetime
    {
        private readonly MsSqlContainer _dbContainer = new MsSqlBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
            .WithPassword("Strong_password_123!")
            .Build();
        public MyContext DbContext  { get; private set; }
        public async Task InitializeAsync()
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
        
            DbContext = new MyContext(options);
            await DbContext.Database.EnsureDeletedAsync();
            await DbContext.Database.EnsureCreatedAsync();
            await DbContext.Database.MigrateAsync();
        }

        public async Task DisposeAsync()
        {
            await _dbContainer.StopAsync();
        }
    }
    public class BrokerFixture : IAsyncLifetime
    {
        private readonly RabbitMqContainer _rabbitMqContainer = new RabbitMqBuilder()
            .WithImage("rabbitmq:3.11")
            .Build();
        public IChannel Channel  { get; private set; }
        public async Task InitializeAsync()
        {
            await _rabbitMqContainer.StartAsync();
            
            var connectionFactory = new ConnectionFactory();
            connectionFactory.Uri = new Uri(_rabbitMqContainer.GetConnectionString());
            using var connection = await connectionFactory.CreateConnectionAsync();
            Channel = await connection.CreateChannelAsync();
        }
        public async Task DisposeAsync()
        {
            await Channel.DisposeAsync();
            await _rabbitMqContainer.StopAsync();
        }
    }
    public class IntegrationTestFixture : IAsyncLifetime
    { 
        public BrokerFixture BrokerFixture { get; }
        public DataBaseFixture DataBaseFixture { get; }
        
        public IntegrationTestFixture()
        {
            BrokerFixture = new BrokerFixture();
            DataBaseFixture = new DataBaseFixture();
        }

        public async Task InitializeAsync()
        {
            await BrokerFixture.InitializeAsync();
            await DataBaseFixture.InitializeAsync();
        }

        public async Task DisposeAsync()
        {
            await BrokerFixture.DisposeAsync();
            await DataBaseFixture.DisposeAsync();
        }
    }

    [CollectionDefinition(nameof(IntegrationCollection))]
    public class IntegrationCollection : ICollectionFixture<IntegrationTestFixture>
    {
    }
    
    [Collection(nameof(IntegrationCollection))]
    // [IntegrationTest]
    public abstract class IntegrationTest : IAsyncLifetime
    {
        protected BrokerFixture BrokerFixture { get; }
        protected DataBaseFixture DataBaseFixture { get; }
        protected MyContext DbContext  { get; }
        protected IChannel IChannel  { get; }
        protected IntegrationTest(IntegrationTestFixture integrationTestFixture)
        {
            BrokerFixture = integrationTestFixture.BrokerFixture;
            DataBaseFixture = integrationTestFixture.DataBaseFixture;
            DbContext = DataBaseFixture.DbContext;
            IChannel = BrokerFixture.Channel;
        }

        public async Task InitializeAsync()
        {
            await BrokerFixture.InitializeAsync();
            await DataBaseFixture.InitializeAsync();
        }

        public async Task DisposeAsync()
        {
            await BrokerFixture.DisposeAsync();
            await DataBaseFixture.DisposeAsync();
        }
    }

    public class ConsumerIntegrationTest : IntegrationTest
    {
        public ConsumerIntegrationTest(IntegrationTestFixture integrationTestFixture) : base(integrationTestFixture)
        {
        }

        [Fact]
        public async Task Given_a_valid_credit_note_renegotiation_should_output_expected_results()
        {
            // Arrange
            var user = new User(0, "LUCIANO PEREIRA", 33, true);
            // Act
            await DbContext.User.AddAsync(user);
            await DbContext.SaveChangesAsync();
            // Assert
            user.Id.Should().Be(1);
        }
        
        // [Fact]
        // public async Task AddAsync_DadosValidos_CriarUsuarioComId()
        // {
        //     IChannel.IsClosed.Should().BeFalse();
        // }
    
        [Fact]
        public async Task TestPublishAndConsumeMessage()
        {
            // Arrange
            var queueName = "test-queue";
            var @event = new CreatedUserEvent(6, "John Doe", 18, false);
            // Act
            var publisher = new Publisher<CreatedUserEvent>(IChannel);
            var subscriber = new Subscriber<CreatedUserEvent>(IChannel);
            await publisher.Send(queueName, @event);
            await subscriber.Consume(queueName);
            // Aguarda o processamento da mensagem
            await Task.Delay(1000); // Tempo para processamento
            // Assert
            subscriber.messageReceived.Should().BeTrue();
            subscriber.receivedEvent.Should().Be(@event);
        
        }
    }
    public class ConsumerV2IntegrationTest : IntegrationTest
    {
        public ConsumerV2IntegrationTest(IntegrationTestFixture integrationTestFixture) : base(integrationTestFixture)
        {
        }

        [Fact]
        public async Task Given_a_valid_credit_note_renegotiation_should_output_expected_results()
        {
            // Arrange
            var user = new User(0, "LUCIANO PEREIRA", 33, true);
            // Act
            await DbContext.User.AddAsync(user);
            await DbContext.SaveChangesAsync();
            // Assert
            user.Id.Should().Be(1);
        }
        // [Fact]
        // public async Task AddAsync_DadosValidos_CriarUsuarioComId()
        // {
        //     IChannel.IsClosed.Should().BeFalse();
        // }
    
        [Fact]
        public async Task TestPublishAndConsumeMessage()
        {
            // Arrange
            var queueName = "test-queue";
            var @event = new CreatedUserEvent(6, "John Doe", 18, false);
            // Act
            var publisher = new Publisher<CreatedUserEvent>(IChannel);
            var subscriber = new Subscriber<CreatedUserEvent>(IChannel);
            await publisher.Send(queueName, @event);
            await subscriber.Consume(queueName);
            // Aguarda o processamento da mensagem
            await Task.Delay(1000); // Tempo para processamento
            // Assert
            subscriber.messageReceived.Should().BeTrue();
            subscriber.receivedEvent.Should().Be(@event);
        }
    }
}