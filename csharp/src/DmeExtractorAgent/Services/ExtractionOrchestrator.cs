using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DmeExtractorAgent.Services;

public class ExtractionOrchestrator
{
    private readonly INlpExtractorClient _extractor;
    private readonly INotificationClient _notifications;
    private readonly IConfiguration _config;
    private readonly ILogger<ExtractionOrchestrator> _logger;

    public ExtractionOrchestrator(
        INlpExtractorClient extractor,
        INotificationClient notifications,
        IConfiguration config,
        ILogger<ExtractionOrchestrator> logger)
    {
        _extractor = extractor;
        _notifications = notifications;
        _config = config;
        _logger = logger;
    }

    // Runs extraction then posts to notifications; returns whether post succeeded
    public async Task<bool> RunOnceAsync(string text)
    {
        var threshold = double.TryParse(_config["Agent:Threshold"], out var t) ? t : 0.45;
        _logger.LogInformation("ExtractionOrchestrator using threshold {Threshold}", threshold);
        try
        {
            var result = await _extractor.ExtractAsync(text, threshold);
            try
            {
                var posted = await _notifications.PostAsync(result);
                return posted;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Notification post failed");
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Extraction failed; skipping notification post");
            return false;
        }
    }
}
