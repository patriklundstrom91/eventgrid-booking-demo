using Domain.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace functions;

public class OutboxDispatcherFunction
{
    private readonly ILogger _logger;
    private readonly HttpClient _http;
    public OutboxDispatcherFunction(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<OutboxDispatcherFunction>();
        _http = new HttpClient();
    }
    [Function("OutboxDispatcher")]
    public async Task Run([TimerTrigger("*/5 * * * * *")] TimerInfo timer)
    {
        List<OutboxItem> pending;

        lock (typeof(OutboxStore))
        {
            pending = OutboxStore.GetAll();
            OutboxStore.Clear();
        }

        if (pending.Count == 0)
        {
            _logger.LogInformation("Outbox empty, nothing to send");
            return;
        }
        _logger.LogInformation("Outbox contains {count} event", pending.Count);
        foreach (var item in pending)
        {
            try
            {
                var json = JsonSerializer.Serialize(item.Body);
                var request = new HttpRequestMessage(
                    HttpMethod.Post,
                    $"http://localhost:7071/runtime/webhooks/EventGrid?functionName={item.FunctionName}"
                )
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };
                request.Headers.Add("aeg-event-type", "Notification");

                var response = await _http.SendAsync(request);
                _logger.LogInformation("Dispatched outbox event to {fn} with status {status}", item.FunctionName, response.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during dispatch of event to {fn}", item.FunctionName);
            }
        }
    }
}