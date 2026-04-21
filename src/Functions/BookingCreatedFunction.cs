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
    public class BookingCreatedFunction
    {
        private readonly EventService _eventService;
        private readonly ILogger<BookingCreatedFunction> _logger;

        public BookingCreatedFunction(EventService eventService, ILogger<BookingCreatedFunction> logger)
        {
            _eventService = eventService ?? throw new ArgumentNullException(nameof(eventService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [Function("BookingCreatedFunction")]
        public async Task Run([EventGridTrigger] EventGridEvent eventGridEvent)
        {
            try
            {
                var id = eventGridEvent.Id;
                if (IdempotencyStore.HasProcessed(id))
                {
                    _logger.LogInformation("Skipping duplicate BookingCreated for {EventId}", id);
                    return;
                }
                IdempotencyStore.MarkProcessed(id);

                _logger.LogInformation("RAW DATA: {Raw}", eventGridEvent.Data?.ToString());

                var dataJson = eventGridEvent.Data?.ToString() ?? string.Empty;
                var created = JsonSerializer.Deserialize<BookingCreatedEvent>(dataJson);
                if (created == null)
                {
                    _logger.LogWarning("Could not deserialize BookingCreatedEvent from EventGrid data. EventId: {EventId}", id);
                    return;
                }

                _logger.LogInformation("BookingCreatedFunction triggered with bookingId: {BookingId}", created.BookingId);

                var validatedEvent = new BookingValidatedEvent(
                    created.BookingId,
                    created.CustomerName,
                    created.CreatedAt,
                    DateTime.UtcNow
                );

                var envelope = new[]
                {
                    new
                    {
                        id = Guid.NewGuid().ToString(),
                        eventType = "BookingValidated",
                        subject = $"booking/{created.BookingId}",
                        eventTime = DateTime.UtcNow,
                        data = validatedEvent,
                        dataVersion = "1.0"
                    }
                };

                OutboxStore.Add(new OutboxItem(
                    FunctionName: "BookingValidatedFunction",
                    Body: envelope
                ));

                _logger.LogInformation("Before SaveAndPush for BookingCreated {BookingId}", created.BookingId);

                var payload = new
                {
                    Type = "BookingCreated",
                    Data = created,
                    Timestamp = DateTime.UtcNow
                };

                await _eventService.SaveAndPushAsync(payload);

                _logger.LogInformation("After SaveAndPush for BookingCreated {BookingId}", created.BookingId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing BookingCreated event");
                throw;
            }
        }
    }
}
