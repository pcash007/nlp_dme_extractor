using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Serilog;
using Microsoft.Extensions.Logging;

namespace DmeExtractorAgent;

public class NlpExtractorClient : DmeExtractorAgent.Services.INlpExtractorClient
{
    private readonly HttpClient _http;

    private readonly ILogger<NlpExtractorClient> _logger;

    public NlpExtractorClient(HttpClient http, IConfiguration config, ILogger<NlpExtractorClient> logger)
    {
        var baseUrl = config["Agent:NlpExtractorUrl"];
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            Log.Error("Missing configuration: Agent:NlpExtractorUrl is not defined. Provide it via appsettings or CLI overrides. Exiting!!");
            System.Environment.Exit(1);
        }
        http.BaseAddress = new Uri(baseUrl);
        _http = http;
        _logger = logger;
    }

    public async Task<object> ExtractAsync(string text, double threshold)
    {
        _logger.LogInformation("Posting received DME text to NLP Extractor API at {BaseAddress}", _http.BaseAddress);
        var payload = new { text };
        var resp = await _http.PostAsJsonAsync("/extract", payload);
        var result = await resp.Content.ReadFromJsonAsync<object>();
        result ??= new { };
        if (resp.IsSuccessStatusCode)
        {
            _logger.LogInformation("NLP Extractor API response {StatusCode}: {Body}", (int)resp.StatusCode, result);
        }
        else
        {
            _logger.LogError("NLP Extractor API error {StatusCode}: {Body}", (int)resp.StatusCode, result);
        }
        return result;

    }
}
