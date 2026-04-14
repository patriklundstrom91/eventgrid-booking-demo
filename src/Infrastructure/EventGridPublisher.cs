// using System.Diagnostics;
// using System.Security.Cryptography.X509Certificates;
// using Azure;
// using Azure.Messaging.EventGrid;
// using Domain.Events;

// namespace Infrastructure;

// public class EventGridPublisher
// {
//     private readonly EventGridPublisherClient _client;
//     public EventGridPublisher(string endpoint, string key)
//     {
//         _client = new EventGridPublisherClient(
//             new Uri(endpoint),
//             new AzureKeyCredential(key)
//         );
//     }
//     public async Task PublishBookingCreatedAsync(BookingCreatedEvent evt)
//     {
//         var eventGridEvent = new EventGridEvent(
//             subject: $"booking/{evt.BookingId}",
//             eventType: "BookingCreated",
//             dataVersion: "1.0",
//             data: evt
//         );
//         await _client.SendEventAsync(eventGridEvent);
//     }
// }
using System.Net.Http.Json;
using Domain.Events;

namespace Infrastructure;

public class EventGridPublisher
{
    private readonly HttpClient _http;

    public EventGridPublisher()
    {
        _http = new HttpClient();
    }

    public async Task PublishBookingCreatedAsync(BookingCreatedEvent evt)
    {
        // Skicka eventet till vår lokala EventGrid-emulator (HttpTrigger)
        var response = await _http.PostAsJsonAsync(
            "http://localhost:7071/api/LocalEventGrid",
            evt
        );

        response.EnsureSuccessStatusCode();
    }
}
