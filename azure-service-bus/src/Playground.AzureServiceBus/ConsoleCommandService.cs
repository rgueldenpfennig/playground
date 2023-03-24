using Microsoft.Extensions.Hosting;
using Playground.AzureServiceBus.Queues;

namespace Playground.AzureServiceBus;

internal class ConsoleCommandService : BackgroundService
{
    private readonly ISimpleQueueProducer _simpleQueueProducer;
    private readonly IHostApplicationLifetime _applicationLifetime;

    public ConsoleCommandService(ISimpleQueueProducer simpleQueueProducer, IHostApplicationLifetime applicationLifetime)
    {
        _simpleQueueProducer = simpleQueueProducer ?? throw new ArgumentNullException(nameof(simpleQueueProducer));
        _applicationLifetime = applicationLifetime ?? throw new ArgumentNullException(nameof(applicationLifetime));
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.Factory.StartNew(async () =>
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (_applicationLifetime.ApplicationStopping.IsCancellationRequested)
                    break;

                Console.WriteLine("Enter text to publish a message:");
                var input = Console.ReadLine();

                if (_applicationLifetime.ApplicationStopping.IsCancellationRequested)
                    break;

                if (string.IsNullOrEmpty(input))
                    continue;

                Console.WriteLine($"Echo: {input}");

                await _simpleQueueProducer.PublishMessageAsync(input, stoppingToken);
            }
        }, stoppingToken);
    }
}
