using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using DmeExtractorAgent.Services;

namespace DmeExtractorAgent;

public static class EntryPoint
{
    public static async Task<int> Main(string[] args)
    {
        // Minimal arg scan (no Spectre): check for --serve and simple overrides.
        var overrides = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        string? dmeText = null;
    string? filePath = null;

        for (int i = 0; i < args.Length; i++)
        {
            var a = args[i];
            switch (a)
            {
                case "--serve":
                    return await Web.HttpServer.RunAsync(args);
                case "--nlp-url":
                    if (i + 1 < args.Length) overrides["Agent:NlpExtractorUrl"] = args[++i];
                    break;
                case "--notifications-url":
                    if (i + 1 < args.Length) overrides["Agent:NotificationUrl"] = args[++i];
                    break;
                case "--threshold":
                    if (i + 1 < args.Length && double.TryParse(args[i + 1], out _)) { overrides["Agent:Threshold"] = args[++i]; }
                    break;
                case "--file":
                    if (i + 1 < args.Length) filePath = args[++i];
                    break;
                default:
                    // First non-option token and rest as text
                    dmeText = string.Join(' ', args.Skip(i));
                    i = args.Length; // exit
                    break;
            }
        }

        return await ManualRunner.RunAsync(dmeText, filePath, overrides);
    }
}

internal static class ManualRunner
{
    public static async Task<int> RunAsync(string? textArg, string? filePath, IDictionary<string, string> overrides)
    {
        
        // Build a lightweight generic host (no WebApplication) and register our core services
        var host = Host.CreateDefaultBuilder()
        .ConfigureAppConfiguration((ctx, cfg) =>
            {
                if (overrides is { Count: > 0 })
                {
                    cfg.AddInMemoryCollection(overrides.Select(kv => new KeyValuePair<string, string?>(kv.Key, kv.Value)));
                }
            })
            .UseSerilog((ctx, services, lc) => lc.WriteTo.Console())
            .ConfigureServices(services =>
            {
                services.AddAgentCoreServices();
            })
            .Build();

        var orchestrator = host.Services.GetRequiredService<ExtractionOrchestrator>();
        var loggerFactory = host.Services.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("ManualRunner");
        logger.LogInformation("Command line Invocation");

        // Prefer file input when provided, otherwise use CLI text, otherwise a sample
        string text;
        if (!string.IsNullOrWhiteSpace(filePath))
        {
            try
            {
                logger.LogInformation("Reading input from file: {Path}", filePath);
                text = await System.IO.File.ReadAllTextAsync(filePath);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to read file: {Path}", filePath);
                return 1;
            }
        }
        else if (!string.IsNullOrWhiteSpace(textArg))
        {
            text = textArg;
        }
        else
        {
            // provide a sample text if none was provided
            text = "Patient needs a CPAP with full face mask and humidifier. AHI > 20. Ordered by Dr. Cameron.";
        }

        var ok = await orchestrator.RunOnceAsync(text);
        if (ok)
            logger.LogInformation("Posted to notification API");
        else
            logger.LogError("Error occured. Look for previous errors in the log");
        return ok ? 0 : 1;
    }
}
