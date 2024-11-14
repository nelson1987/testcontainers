using System.Text;
using System.Text.Json;
using RabbitMQ.Client;

namespace Presentation.Commons;

public interface IPublisher<in T> where T : class
{
    Task Send(string queueName, T @event);
}

public class Publisher<T> : IPublisher<T> where T : class
{
    private readonly ILogger<Publisher<T>> _logger;
    private readonly IChannel _channel;

    public Publisher(IChannel connection, ILogger<Publisher<T>> logger)
    {
        _logger = logger;
        _channel = connection;
    }

    public async Task Send(string queueName, T @event)
    {
        _logger.LogInformation($"Sending message to {queueName}");
        await _channel.QueueDeclareAsync(queueName, durable: true, exclusive: false, autoDelete: false);

        var message = JsonSerializer.Serialize(@event);
        var body = System.Text.Encoding.UTF8.GetBytes(message);
        var properties = new BasicProperties
        {
            Persistent = true
        };

        _logger.LogInformation($"Publishing in {queueName}, event: {message}");
        await _channel.BasicPublishAsync(exchange: string.Empty,
            routingKey: queueName,
            mandatory: true,
            basicProperties: properties,
            body: body);
    }
}

public abstract class RabbitMqClientBase : IAsyncDisposable
{
    protected abstract string QueueName { get; }
    protected IChannel Channel { get; private set; }
    private IConnection _connection;
    private readonly ConnectionFactory _connectionFactory;
    private readonly ILogger<RabbitMqClientBase> _logger;

    protected RabbitMqClientBase(ConnectionFactory connectionFactory,
        ILogger<RabbitMqClientBase> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
        Connect();
    }

    private async Task Connect()
    {
        if (_connection == null || _connection.IsOpen == false)
        {
            _connection = await _connectionFactory.CreateConnectionAsync();
        }

        if (Channel == null || Channel.IsOpen == false)
        {
            Channel = await _connection.CreateChannelAsync();
            await Channel.QueueDeclareAsync(queue: QueueName, durable: false, exclusive: false, autoDelete: false);
        }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            await Channel?.CloseAsync();
            Channel?.Dispose();
            Channel = null;

            await _connection?.CloseAsync();
            _connection?.Dispose();
            _connection = null;
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Cannot dispose RabbitMQ channel or connection");
        }
    }
}

public interface IMessageProducer<T> where T : class
{
    Task SendMessage(T message);
}

public abstract class ProducerBase<T> : RabbitMqClientBase, IMessageProducer<T> where T : class
{
    private readonly ILogger<ProducerBase<T>> _logger;

    protected ProducerBase(ConnectionFactory connectionFactory,
        ILogger<RabbitMqClientBase> logger,
        ILogger<ProducerBase<T>> producerBaseLogger) : base(connectionFactory, logger)
    {
        _logger = producerBaseLogger;
    }

    public virtual async Task SendMessage(T message)
    {
        try
        {
            var body = Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(message));
            var properties = new BasicProperties()
                {
                    ContentType = "application/json",
                    //DeliveryMode = 1; // Doesn't persist to disk
                    Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
                    Persistent = true
                };

            await Channel.BasicPublishAsync(exchange: string.Empty,
                routingKey: QueueName,
                mandatory: true,
                basicProperties: properties,
                body: body);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Error while publishing");
        }
    }
}

public class CheckoutItemProducer : ProducerBase<CreatedUserEvent>
{
    public CheckoutItemProducer(ConnectionFactory connectionFactory,
        ILogger<RabbitMqClientBase> logger,
        ILogger<ProducerBase<CreatedUserEvent>> producerBaseLogger)
        : base(connectionFactory, logger, producerBaseLogger)
    {
    }

    protected override string QueueName => "stock-validator";
}