using Azure.Messaging.ServiceBus;

namespace Playground.AzureServiceBus.Queues;

public interface ISimpleQueueProducer
{
    Task PublishMessageAsync(string message, CancellationToken cancellationToken);
}

public class SimpleQueueProducer : ISimpleQueueProducer, IAsyncDisposable
{
    private readonly ServiceBusSender _serviceBusSender;

    public SimpleQueueProducer(ServiceBusClient serviceBusClient)
    {
        if (serviceBusClient is null)
        {
            throw new ArgumentNullException(nameof(serviceBusClient));
        }

        _serviceBusSender = serviceBusClient.CreateSender("queue-simple");
    }

    public async Task PublishMessageAsync(string message, CancellationToken cancellationToken)
    {
        await _serviceBusSender.SendMessageAsync(new ServiceBusMessage(message), cancellationToken);
    }

    public ValueTask DisposeAsync()
    {
        return _serviceBusSender.DisposeAsync();
    }
}
