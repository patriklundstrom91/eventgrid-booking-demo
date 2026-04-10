// Default URL for triggering event grid function in the local environment.
// http://localhost:7071/runtime/webhooks/EventGrid?functionName={functionname}

using System;
using Azure.Messaging;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace functions;

public class BookingProcessedFunction
{
    private readonly ILogger<BookingProcessedFunction> _logger;

    public BookingProcessedFunction(ILogger<BookingProcessedFunction> logger)
    {
        _logger = logger;
    }

    [Function(nameof(BookingProcessedFunction))]
    public void Run([EventGridTrigger] CloudEvent cloudEvent)
    {
        _logger.LogInformation("Event type: {type}, Event subject: {subject}", cloudEvent.Type, cloudEvent.Subject);
    }
}