using Serilog;
using Microsoft.Extensions.Logging;
using DmeExtractorAgent.Services;

namespace DmeExtractorAgent.Web;

// DTO for POST /process
public record Request(string Text);

public static class HttpServer
{
    private static WebApplication? _webApp;
    private static readonly object _gate = new();

    public static WebApplication GetOrBuild(string[] args)
    {
        if (_webApp != null)
            return _webApp;
        lock (_gate)
        {
            if (_webApp == null)
            {
                _webApp = Build(args);
            }
            return _webApp;
        }
    }

    public static WebApplication Build(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Logging
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateLogger();

        builder.Host.UseSerilog();

    // Core services used by both web and non-web paths
        builder.Services.AddAgentCoreServices();

        var app = builder.Build();

        // Log each HTTP request (method, path, status, timing)
        app.UseSerilogRequestLogging();

        // HTTP endpoint: orchestrates extraction and notification
        app.MapPost("/process", async (Request dmeText, ExtractionOrchestrator orchestrator, ILoggerFactory loggerFactory) =>
        {
            var log = loggerFactory.CreateLogger("HttpServer");
            log.LogInformation("/process invoked");
            var posted = await orchestrator.RunOnceAsync(dmeText.Text);
            log.LogInformation("/process completed. Posted: {Posted}", posted);
            return Results.Ok(new { posted });
        });

        return app;
    }

    // Starts the HTTP listener using the local Build
    public static async Task<int> RunAsync(string[] args)
    {
        var app = GetOrBuild(args);
        await app.RunAsync();
        return 0;
    }
}
