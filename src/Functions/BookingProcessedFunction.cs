// BookingProcessedFunction.cs
using System;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Messaging.EventGrid;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Domain.Events;

namespace functions
{
    public class BookingProcessedFunction
    {
        private readonly EventService _eventService;
        private readonly ILogger<BookingProcessedFunction> _logger;

        public BookingProcessedFunction(EventService eventService, ILogger<BookingProcessedFunction> logger)
        {
            _eventService = eventService ?? throw new ArgumentNullException(nameof(eventService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [Function("BookingProcessedFunction")]
        public async Task Run([EventGridTrigger] EventGridEvent eventGridEvent)
        {
            try
            {
                var id = eventGridEvent.Id;
                if (IdempotencyStore.HasProcessed(id))
                {
                    _logger.LogInformation("Skipping duplicate BookingProcessed for {EventId}", id);
                    return;
                }
                IdempotencyStore.MarkProcessed(id);

                var json = eventGridEvent.Data?.ToString() ?? string.Empty;
                var processed = JsonSerializer.Deserialize<BookingProcessedEvent>(json);
                if (processed == null)
                {
                    _logger.LogWarning("Could not deserialize BookingProcessedEvent. EventId: {EventId} Payload: {Payload}", id, json);
                    return;
                }

                _logger.LogInformation("BookingProcessedFunction triggered for bookingId: {BookingId}", processed.BookingId);

                var payload = new
                {
                    Type = "BookingProcessed",
                    Data = processed,
                    Timestamp = DateTime.UtcNow
                };

                await _eventService.SaveAndPushAsync(payload);

                _logger.LogInformation("Finished processing BookingProcessed for bookingId: {BookingId}", processed.BookingId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing BookingProcessed event");
                throw;
            }
        }
    }
}
