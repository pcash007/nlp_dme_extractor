using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;

namespace DmeExtractorAgent.Services.Http;

public class NotificationClient : DmeExtractorAgent.Services.INotificationClient
{
    private readonly HttpClient _http;
    private readonly ILogger<NotificationClient> _logger;

    public NotificationClient(HttpClient http, IConfiguration config, ILogger<NotificationClient> logger)
    {
        var baseUrl = config["Agent:NotificationUrl"];
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            Log.Error("Missing configuration: Agent:NotificationUrl is not defined. Provide it via appsettings or CLI overrides. Exiting!!");
            System.Environment.Exit(1);
        }
        http.BaseAddress = new Uri(baseUrl);
        _http = http;
        _logger = logger;
    }

    public async Task<bool> PostAsync(object result)
    {
        _logger.LogInformation("Posting extracted result to notification API at {BaseAddress}", _http.BaseAddress);
        var resp = await _http.PostAsJsonAsync("", result);
        var body = await resp.Content.ReadAsStringAsync();
        if (resp.IsSuccessStatusCode)
        {
            _logger.LogInformation("Notification API response {StatusCode}: {Body}", (int)resp.StatusCode, body);
        }
        else
        {
            _logger.LogError("Notification API error {StatusCode}: {Body}", (int)resp.StatusCode, body);
        }
        return resp.IsSuccessStatusCode;
    }
}
