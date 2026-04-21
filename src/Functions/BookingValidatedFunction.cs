using System;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Messaging.EventGrid;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Domain.Events;
using Domain.Models;

namespace functions
{
    public class BookingValidatedFunction
    {
        private readonly EventService _eventService;
        private readonly ILogger<BookingValidatedFunction> _logger;

        public BookingValidatedFunction(EventService eventService, ILogger<BookingValidatedFunction> logger)
        {
            _eventService = eventService ?? throw new ArgumentNullException(nameof(eventService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [Function("BookingValidatedFunction")]
        public async Task Run([EventGridTrigger] EventGridEvent eventGridEvent)
        {
            try
            {
                var id = eventGridEvent.Id;
                if (IdempotencyStore.HasProcessed(id))
                {
                    _logger.LogInformation("Skipping duplicate BookingValidated for {EventId}", id);
                    return;
                }
                IdempotencyStore.MarkProcessed(id);

                var json = eventGridEvent.Data?.ToString() ?? string.Empty;
                var validated = JsonSerializer.Deserialize<BookingValidatedEvent>(json);
                if (validated == null)
                {
                    _logger.LogWarning("Could not deserialize BookingValidatedEvent. EventId: {EventId} Payload: {Payload}", id, json);
                    return;
                }

                _logger.LogInformation("BookingValidatedFunction triggered for bookingId: {BookingId}", validated.BookingId);

                var processedEvent = new BookingProcessedEvent(
                    validated.BookingId,
                    validated.CustomerName,
                    validated.CreatedAt,
                    validated.ValidatedAt,
                    DateTime.UtcNow
                );

                var envelope = new[]
                {
                    new
                    {
                        id = Guid.NewGuid().ToString(),
                        eventType = "BookingProcessed",
                        subject = $"booking/{validated.BookingId}",
                        eventTime = DateTime.UtcNow,
                        data = processedEvent,
                        dataVersion = "1.0"
                    }
                };

                OutboxStore.Add(new OutboxItem(
                    FunctionName: "BookingProcessedFunction",
                    Body: envelope
                ));

                _logger.LogInformation("Persisted outbox item and about to push BookingValidated event for bookingId: {BookingId}", validated.BookingId);

                var payload = new
                {
                    Type = "BookingValidated",
                    Data = validated,
                    Timestamp = DateTime.UtcNow
                };

                await _eventService.SaveAndPushAsync(payload);

                _logger.LogInformation("Finished processing BookingValidated for bookingId: {BookingId}", validated.BookingId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing BookingValidated event");
                throw;
            }
        }
    }
}
