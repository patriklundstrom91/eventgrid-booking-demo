// SignalRNegotiateService.cs
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.SignalR.Management;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace functions
{
    public class SignalRNegotiateService
    {
        private readonly IServiceHubContext? _hubContext;
        private readonly ServiceManager? _serviceManager;
        private readonly string? _connectionString;
        private readonly ILogger<SignalRNegotiateService> _logger;

        public SignalRNegotiateService(IServiceHubContext? hubContext, ServiceManager? serviceManager, ILogger<SignalRNegotiateService> logger)
        {
            _hubContext = hubContext;
            _serviceManager = serviceManager;
            _logger = logger;
            _connectionString = Environment.GetEnvironmentVariable("AZURE_SIGNALR_CONNECTION_STRING");
        }

        public async Task<object?> GetClientEndpointAsync(string hubName)
        {
            // 1) Försök reflection mot ServiceManager (om SDK exponerar metoden)
            if (_serviceManager != null)
            {
                try
                {
                    var smType = _serviceManager.GetType();
                    var method = smType.GetMethod("GetClientEndpointAsync", new[] { typeof(string), typeof(CancellationToken) })
                                 ?? smType.GetMethod("GetClientEndpointAsync", new[] { typeof(string) });

                    if (method != null)
                    {
                        var args = method.GetParameters().Length == 2
                            ? new object?[] { hubName, CancellationToken.None }
                            : new object?[] { hubName };

                        var result = method.Invoke(_serviceManager, args);
                        if (result is Task task)
                        {
                            await task.ConfigureAwait(false);
                            var resultProp = task.GetType().GetProperty("Result");
                            return resultProp?.GetValue(task);
                        }
                        return result;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "ServiceManager.GetClientEndpointAsync via reflection misslyckades.");
                }
            }

            // 2) Försök reflection mot hubContext (nyare SDK kan ha metoden här)
            if (_hubContext != null)
            {
                try
                {
                    var hcType = _hubContext.GetType();
                    var method = hcType.GetMethod("GetClientEndpointAsync", new[] { typeof(CancellationToken) })
                                 ?? hcType.GetMethod("GetClientEndpointAsync", Type.EmptyTypes);

                    if (method != null)
                    {
                        var args = method.GetParameters().Length == 1
                            ? new object?[] { CancellationToken.None }
                            : Array.Empty<object?>();

                        var result = method.Invoke(_hubContext, args);
                        if (result is Task task)
                        {
                            await task.ConfigureAwait(false);
                            var resultProp = task.GetType().GetProperty("Result");
                            return resultProp?.GetValue(task);
                        }
                        return result;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "IServiceHubContext.GetClientEndpointAsync via reflection misslyckades.");
                }
            }

            // 3) Fallback: bygg Url + AccessToken från connection string (manuell JWT)
            if (!string.IsNullOrWhiteSpace(_connectionString))
            {
                try
                {
                    var parsed = ParseConnectionString(_connectionString);
                    if (!parsed.TryGetValue("Endpoint", out var endpoint) || !parsed.TryGetValue("AccessKey", out var accessKey))
                    {
                        _logger.LogError("Connection string saknar Endpoint eller AccessKey.");
                        return null;
                    }

                    // Audience / hub url
                    var hubUrl = $"{endpoint.TrimEnd('/')}/client/?hub={Uri.EscapeDataString(hubName)}";

                    // Skapa JWT med kort giltighetstid (t.ex. 1 minut)
                    var token = CreateJwtToken(hubUrl, accessKey, TimeSpan.FromMinutes(1));

                    // Returnera ett anonymt objekt med Url och AccessToken (samma shape som SDK)
                    return new { Url = hubUrl, AccessToken = token };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to build negotiate response from connection string.");
                    return null;
                }
            }

            _logger.LogError("Kunde inte hitta någon GetClientEndpointAsync/GetClientEndpoint‑metod på ServiceManager eller IServiceHubContext, och ingen connection string fanns.");
            return null;
        }

        private static string CreateJwtToken(string audience, string base64Key, TimeSpan ttl)
        {
            var keyBytes = Convert.FromBase64String(base64Key);
            var securityKey = new SymmetricSecurityKey(keyBytes);
            var creds = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var now = DateTime.UtcNow;
            var handler = new JwtSecurityTokenHandler();
            var token = handler.CreateJwtSecurityToken(
                issuer: null,
                audience: audience,
                subject: new ClaimsIdentity(new[] { new Claim("aud", audience) }),
                notBefore: now,
                expires: now.Add(ttl),
                issuedAt: now,
                signingCredentials: creds);

            return handler.WriteToken(token);
        }

        private static System.Collections.Generic.Dictionary<string, string> ParseConnectionString(string cs)
        {
            var dict = new System.Collections.Generic.Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var parts = cs.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var p in parts)
            {
                var idx = p.IndexOf('=');
                if (idx <= 0) continue;
                var k = p.Substring(0, idx).Trim();
                var v = p.Substring(idx + 1).Trim();
                dict[k] = v;
            }
            return dict;
        }
    }
}
