using System.Net;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Playground.AzureServiceBus.Queues;

namespace Playground.AzureServiceBus.Api;

internal static class QueuesApi
{
    public static RouteGroupBuilder MapQueues(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/queues");

        group.WithTags("Queues");
        group.WithOpenApi();

        group.MapPost("/simple", async Task<StatusCodeHttpResult> (ISimpleQueueProducer producer, [FromBody] string message, CancellationToken cancellationToken) =>
        {
            await producer.PublishMessageAsync(message, cancellationToken);
            return TypedResults.StatusCode((int)HttpStatusCode.Created);
        })
        .Produces((int)HttpStatusCode.Created)
        .WithDescription("Publishes a message to a simple Azure Service Bus queue.");

        return group;
    }
}

