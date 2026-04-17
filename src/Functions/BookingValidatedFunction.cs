using Azure.Messaging.EventGrid;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Domain.Events;
using System.Text.Json;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace functions
{
    public class BookingValidatedFunction
    {
        private readonly ILogger _logger;
        private readonly HttpClient _http;


        public BookingValidatedFunction(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<BookingValidatedFunction>();
            _http = new HttpClient();
        }

        [Function("BookingValidatedFunction")]
        public async Task Run([EventGridTrigger] EventGridEvent eventGridEvent)
        {
            var id = eventGridEvent.Id;
            if (IdempotencyStore.HasProcessed(id))
            {
                _logger.LogInformation($"Skipping duplicate BookingValidated for {id}");
                return;
            }
            IdempotencyStore.MarkProcessed(id);

            var json = eventGridEvent.Data.ToString();
            var validated = JsonSerializer.Deserialize<BookingValidatedEvent>(json);

            _logger.LogInformation(
                "BookingValidatedFunction triggered: {bookingId} validated at {time}",
                validated.BookingId,
                validated.ValidatedAt
            );

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
            var jsonOut = JsonSerializer.Serialize(envelope);

            var request = new HttpRequestMessage(
                HttpMethod.Post,
                "http://localhost:7071/runtime/webhooks/EventGrid?functionName=BookingProcessedFunction"
            )
            {
                Content = new StringContent(jsonOut, Encoding.UTF8, "application/json")
            };

            request.Headers.Add("aeg-event-type", "Notification");

            var response = await _http.SendAsync(request);

            _logger.LogInformation("Forwarded BookingProcessed event: {status}", response.StatusCode);

            EventLogger.LogEvent(new
            {
                Type = "BookingValidated",
                Data = validated,
                Timestamp = DateTime.UtcNow
            });
        }
    }
}
