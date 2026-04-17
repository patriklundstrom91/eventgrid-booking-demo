using Azure.Messaging.EventGrid;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Domain.Events;
using System.Text.Json;
using System;

namespace functions
{
    public class BookingProcessedFunction
    {
        private readonly ILogger _logger;
        public BookingProcessedFunction(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<BookingProcessedFunction>();
        }
        [Function("BookingProcessedFunction")]
        public void Run([EventGridTrigger] EventGridEvent eventGridEvent)
        {
            var id = eventGridEvent.Id;
            if (IdempotencyStore.HasProcessed(id))
            {
                _logger.LogInformation($"Skipping duplicate BookingProcessed for {id}");
                return;
            }
            IdempotencyStore.MarkProcessed(id);
            var json = eventGridEvent.Data.ToString();
            var processed = JsonSerializer.Deserialize<BookingProcessedEvent>(json);

            _logger.LogInformation(
                "BookingProcessedFunction triggered: Booking {bookingId} processed at {time}",
                processed.BookingId,
                processed.ProcessedAt
            );

            EventLogger.LogEvent(new
            {
                Type = "BookingProcessed",
                Data = processed,
                Timestamp = DateTime.UtcNow
            });
        }
    }
}