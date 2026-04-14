using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace functions
{
    public class LocalEventGrid
    {
        private readonly ILogger _logger;

        public LocalEventGrid(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<LocalEventGrid>();
        }

        [Function("LocalEventGrid")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req,
            FunctionContext context)
        {
            var body = await req.ReadAsStringAsync();
            _logger.LogInformation("Local EventGrid received event: {body}", body);

            var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
            await response.WriteStringAsync("Local EventGrid received event.");
            return response;
        }
    }
}