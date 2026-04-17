using Azure.Messaging.EventGrid;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Domain.Events;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using System;
using System.Text;

namespace functions
{
    public class BookingCreatedFunction
    {
        private readonly ILogger _logger;
        private readonly HttpClient _http;

        public BookingCreatedFunction(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<BookingCreatedFunction>();
            _http = new HttpClient();
        }

        [Function("BookingCreatedFunction")]
        public async Task Run([EventGridTrigger] EventGridEvent eventGridEvent)
        {
            var id = eventGridEvent.Id;
            if (IdempotencyStore.HasProcessed(id))
            {
                _logger.LogInformation($"Skipping duplicate BookingCreated for {id}");
                return;
            }
            IdempotencyStore.MarkProcessed(id);

            _logger.LogInformation("RAW DATA: " + eventGridEvent.Data.ToString());

            _logger.LogInformation("BookingCreatedFunction triggered with event: {data}", eventGridEvent.Data.ToString());

            var created = JsonSerializer.Deserialize<BookingCreatedEvent>(eventGridEvent.Data.ToString());

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
            var json = JsonSerializer.Serialize(envelope);

            var request = new HttpRequestMessage(
                HttpMethod.Post,
                "http://localhost:7071/runtime/webhooks/EventGrid?functionName=BookingValidatedFunction"
            )
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            request.Headers.Add("aeg-event-type", "Notification");

            var response = await _http.SendAsync(request);

            _logger.LogInformation("Forwarded BookingValidated event: {status}", response.StatusCode);

            _logger.LogInformation("BEFORE LOGEVENT");

            EventLogger.LogEvent(new
            {
                Type = "BookingCreated",
                Data = created,
                Timestamp = DateTime.UtcNow
            });

            _logger.LogInformation("AFTER LOGEVENT");

        }
    }
}
