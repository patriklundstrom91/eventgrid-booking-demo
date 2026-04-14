// Default URL for triggering event grid function in the local environment.
// http://localhost:7071/runtime/webhooks/EventGrid?functionName={functionname}

using Azure.Messaging.EventGrid;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace functions
{
    public class BookingCreatedFunction
    {
        private readonly ILogger _logger;

        public BookingCreatedFunction(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<BookingCreatedFunction>();
        }

        [Function("BookingCreatedFunction")]
        public void Run([EventGridTrigger] EventGridEvent eventGridEvent)
        {
            _logger.LogInformation("BookingCreatedFunction triggered with event: {data}", eventGridEvent.Data.ToString());
        }
    }
}
