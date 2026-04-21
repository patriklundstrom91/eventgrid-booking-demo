using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.SignalR.Management;
using Microsoft.Extensions.Logging;

namespace functions
{
    public class NegotiateFunction
    {
        private readonly ServiceHubContext _hubContext;
        private readonly ILogger<NegotiateFunction> _logger;

        public NegotiateFunction(ServiceHubContext hubContext, ILogger<NegotiateFunction> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        [Function("negotiate")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
        {
            var negotiate = await _hubContext.NegotiateAsync();

            var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");

            await response.WriteStringAsync(JsonSerializer.Serialize(new
            {
                Url = negotiate.Url,
                AccessToken = negotiate.AccessToken
            }));

            return response;
        }
    }
}