using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Domain.Events;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace functions
{
    public class LocalEventGrid
    {
        private readonly ILogger _logger;
        private readonly HttpClient _http;

        public LocalEventGrid(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<LocalEventGrid>();
            _http = new HttpClient();
        }

        [Function("LocalEventGrid")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req,
            FunctionContext context)
        {
            var body = await req.ReadAsStringAsync();
            _logger.LogInformation("Local EventGrid received event: {body}", body);

            if (string.IsNullOrWhiteSpace(body))
            {
                var bad = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                await bad.WriteStringAsync("Empty body received.");
                return bad;
            }

            // FIX 1: Case-insensitive JSON
            var bookingEvent = JsonSerializer.Deserialize<BookingCreatedEvent>(
                body,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            if (bookingEvent == null)
            {
                var bad = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                await bad.WriteStringAsync("Invalid JSON payload.");
                return bad;
            }

            // FIX 2: Correct Event Grid envelope
            var eventGridEnvelope = new[]
            {
                new
                {
                    id = Guid.NewGuid().ToString(),
                    eventType = "BookingCreated",
                    subject = $"booking/{bookingEvent.BookingId}",
                    eventTime = DateTime.UtcNow,
                    data = bookingEvent,
                    dataVersion = "1.0"
                }
            };

            var json = JsonSerializer.Serialize(eventGridEnvelope);
            _logger.LogInformation("Forwarding EventGrid envelope: {json}", json);

            // FIX 3: Correct EventGridTrigger headers
            var requestMessage = new HttpRequestMessage(
                HttpMethod.Post,
                "http://localhost:7071/runtime/webhooks/EventGrid?functionName=BookingCreatedFunction")
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            requestMessage.Headers.Add("aeg-event-type", "Notification");

            // FIX 4: Send request manually
            var response = await _http.SendAsync(requestMessage);

            _logger.LogInformation("EventGrid webhook response: {status}", response.StatusCode);

            var httpResponse = req.CreateResponse(response.IsSuccessStatusCode
                ? System.Net.HttpStatusCode.OK
                : System.Net.HttpStatusCode.InternalServerError);

            await httpResponse.WriteStringAsync(
                response.IsSuccessStatusCode
                    ? "Event forwarded to BookingCreatedFunction."
                    : "Failed to forward event."
            );

            return httpResponse;
        }
    }
}
