namespace DmeExtractorAgent.Services;

public interface INlpExtractorClient
{
    Task<object> ExtractAsync(string text, double threshold);
}

public interface INotificationClient
{
    Task<bool> PostAsync(object result);
}
