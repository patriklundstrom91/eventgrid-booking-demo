using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.SignalR.Management;
using Microsoft.Extensions.Logging;

namespace functions
{
    public class EventService
    {

        private readonly string _eventsFilePath;
        private readonly object _fileLock = new object();
        private const string HubMethodName = "event";

        private readonly ServiceHubContext _hubContext;
        private readonly ILogger<EventService> _logger;

        public EventService(ServiceHubContext hubContext, ILogger<EventService> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
            _eventsFilePath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "events.json"));
        }

        public async Task SaveAndPushAsync(object evt)
        {
            if (evt == null)
            {
                _logger.LogWarning("SaveAndPushAsync called with null evt");
                return;
            }

            try
            {
                List<object> events = new List<object>();
                lock (_fileLock)
                {
                    if (File.Exists(_eventsFilePath))
                    {
                        var json = File.ReadAllText(_eventsFilePath);
                        if (!string.IsNullOrWhiteSpace(json))
                        {
                            try { events = JsonSerializer.Deserialize<List<object>>(json) ?? new List<object>(); }
                            catch (Exception ex) { _logger.LogWarning(ex, "Could not deserialize existing events.json, starting fresh list."); events = new List<object>(); }
                        }
                    }
                    events.Add(evt);
                    var outJson = JsonSerializer.Serialize(events, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(_eventsFilePath, outJson);
                }
                _logger.LogInformation("Event saved to events.json");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to persist event to events.json");
            }

            try
            {
                await _hubContext.Clients.All.SendCoreAsync(HubMethodName, new object[] { evt }, CancellationToken.None);
                _logger.LogInformation("SignalR push sent");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SignalR push failed");
            }
        }
    }
}