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
        // Minimal arg scan (no Spectre): check for --serve and simple overrides
        var overrides = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        string? dmeText = null;

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
                    if (i + 1 < args.Length && double.TryParse(args[i + 1], out var th)) { overrides["Agent:Threshold"] = args[++i]; }
                    break;
                default:
                    // First non-option token and rest as text
                    dmeText = string.Join(' ', args.Skip(i));
                    i = args.Length; // exit
                    break;
            }
        }

        return await ManualRunner.RunAsync(dmeText, overrides);
    }
}

internal static class ManualRunner
{
    public static async Task<int> RunAsync(string? textArg, IDictionary<string, string> overrides)
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

        var text = string.IsNullOrWhiteSpace(textArg)
            ? "Patient needs a CPAP with full face mask and humidifier. AHI > 20. Ordered by Dr. Cameron."
            : textArg;

        var ok = await orchestrator.RunOnceAsync(text);
        if (ok)
            logger.LogInformation("Posted to notification API");
        else
            logger.LogError("Failed to post to notification API");
        return ok ? 0 : 1;
    }
}
