using System.Text;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Playground.AzureServiceBus.Queues;

public class SimpleQueueConsumer : BackgroundService
{
    private readonly ServiceBusProcessor _processor;
    private readonly ILogger<SimpleQueueConsumer> _logger;

    public SimpleQueueConsumer(ServiceBusClient serviceBusClient, ILogger<SimpleQueueConsumer> logger)
    {
        if (serviceBusClient is null)
        {
            throw new ArgumentNullException(nameof(serviceBusClient));
        }

        _processor = serviceBusClient.CreateProcessor("queue-simple");
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _processor.ProcessMessageAsync += OnProcessMessageAsync;
        _processor.ProcessErrorAsync += OnProcessErrorAsync;

        _logger.LogInformation("Start processing queue...");
        await _processor.StartProcessingAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(100);
        }

        _logger.LogInformation("Stop processing queue...");
        await _processor.StopProcessingAsync();
        _logger.LogInformation("Stopped processing queue");
    }

    private async Task OnProcessMessageAsync(ProcessMessageEventArgs args)
    {
        var memory = args.Message.Body.ToMemory();
        var body = Encoding.UTF8.GetString(memory.Span);

        _logger.LogInformation("Receiving message created at {EnqueuedTime:s} ({Size} bytes)", args.Message.EnqueuedTime, memory.Length);
        _logger.LogInformation("Body: {Body}", body);

        if (body.Equals("dead", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("Moving message to dead letter queue...");
            await args.DeadLetterMessageAsync(args.Message);
        }
        else
        {
            await args.CompleteMessageAsync(args.Message);
        }
    }

    private Task OnProcessErrorAsync(ProcessErrorEventArgs args)
    {
        _logger.LogError("A processing error occurred", args.Exception);
        return Task.CompletedTask;
    }
}
