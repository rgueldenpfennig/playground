using System.Threading.Channels;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Hosting;

namespace Playground.AzureServiceBus.Queues;

public interface ISimpleQueueProducer
{
    Task PublishMessageAsync(string message);
}

internal class SimpleQueueProducer : BackgroundService, ISimpleQueueProducer, IAsyncDisposable
{
    private readonly Channel<string> _channel = Channel.CreateUnbounded<string>();
    private readonly ServiceBusSender _serviceBusSender;

    public SimpleQueueProducer(ServiceBusClient serviceBusClient)
    {
        if (serviceBusClient is null)
        {
            throw new ArgumentNullException(nameof(serviceBusClient));
        }

        _serviceBusSender = serviceBusClient.CreateSender("queue-simple");
    }

    public Task PublishMessageAsync(string message)
    {
        return _channel.Writer.WriteAsync(message).AsTask();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var message = await _channel.Reader.ReadAsync(stoppingToken);
            if (message is not null)
            {
                await _serviceBusSender.SendMessageAsync(new ServiceBusMessage(message), stoppingToken);
            }
        }
    }

    public ValueTask DisposeAsync()
    {
        return _serviceBusSender.DisposeAsync();
    }
}
