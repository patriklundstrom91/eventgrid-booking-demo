// Program.cs (dotnet-isolated)
using System.Threading;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Azure.SignalR.Management;
using Microsoft.Extensions.Logging;
using functions;
using System;

var host = new HostBuilder()
  .ConfigureFunctionsWorkerDefaults()
  .ConfigureServices((ctx, services) =>
  {
    var conn = Environment.GetEnvironmentVariable("AZURE_SIGNALR_CONNECTION_STRING");
    var manager = new ServiceManagerBuilder().WithOptions(o => o.ConnectionString = conn).BuildServiceManager();
    var hubContext = manager.CreateHubContextAsync("events", CancellationToken.None).GetAwaiter().GetResult();

    services.AddSingleton(hubContext);
    services.AddSingleton(manager);
    services.AddSingleton<EventService>();
    services.AddSingleton<SignalRNegotiateService>();
    services.AddLogging();
  })
  .Build();

host.Run();
