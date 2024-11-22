using Domain;
using Infrastructure;
using RabbitMQ.Client;

namespace IntegrationTests;

[Collection("Shared Infrastructure")]
public class SharedInfrastructure : IntegrationTestBase, IAsyncDisposable
{
    protected readonly IChannel Channel;
    protected readonly IConsumer<CreatedOrderEvent> CreatedOrderConsumer;

    public SharedInfrastructure(SharedTestInfrastructure infrastructure)
        : base(infrastructure)
    {
        Channel = RabbitConnection.CreateChannelAsync()
            .GetAwaiter()
            .GetResult();
        Channel.QueueDeclareAsync(typeof(CreatedOrderEvent).FullName!, durable: true, exclusive: false,
                autoDelete: false)
            .GetAwaiter()
            .GetResult();
        CreatedOrderConsumer = new Consumer<CreatedOrderEvent>(Channel);
    }

    public async ValueTask DisposeAsync()
    {
        await Channel.DisposeAsync();
        await Channel.QueueDeleteAsync(typeof(CreatedOrderEvent).FullName!);
    }
}