using RabbitMQ.Client;

namespace IntegrationTests;

[Collection("Shared Infrastructure")]
public class SharedInfrastructure : IntegrationTestBase, IAsyncDisposable
{
    protected readonly IChannel Channel;

    public SharedInfrastructure(SharedTestInfrastructure infrastructure)
        : base(infrastructure)
    {
        Channel = RabbitConnection.CreateChannelAsync().GetAwaiter().GetResult();
    }

    public async ValueTask DisposeAsync()
    {
        await Channel.DisposeAsync();
    }
}