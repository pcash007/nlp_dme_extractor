using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using DmeExtractorAgent.Web;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Xunit;

namespace DmeExtractorAgent.Tests;

public class EntryPointProcessTests
{
    [Fact]
    public async Task Main_With_Serve_Starts_Server_And_Handles_Request()
    {
        // Arrange stub services the app will call
        await using var extractor = await StartStubApp(
            map: app => app.MapPost("/extract", () => Results.Json(new { mentions = Array.Empty<object>() }))
        );
        await using var notifier = await StartStubApp(
            map: app => app.MapPost("/", () => Results.Ok())
        );

        var solutionDir = FindSolutionDir();
        var projectPath = Path.Combine(solutionDir, "src", "DmeExtractorAgent");
        var port = GetFreeTcpPort();
        var baseUrl = $"http://127.0.0.1:{port}";

        using var proc = new Process();
        proc.StartInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --project \"{projectPath}\" -- --serve",
            WorkingDirectory = solutionDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        proc.StartInfo.Environment["ASPNETCORE_URLS"] = baseUrl;
        proc.StartInfo.Environment["DOTNET_ENVIRONMENT"] = "Production";
        // Provide configuration to clients via environment variables
        proc.StartInfo.Environment["Agent__NlpExtractorUrl"] = extractor.BaseAddress;
        proc.StartInfo.Environment["Agent__NotificationUrl"] = notifier.BaseAddress;

        proc.Start();

        try
        {
            // Wait for server to become ready and respond
            using var client = new HttpClient();
            var processUrl = new Uri(new Uri(baseUrl), "/process");
            var started = await WaitUntilAsync(async () =>
            {
                try
                {
                    var resp = await client.PostAsJsonAsync(processUrl, new { text = "hello" });
                    return resp.StatusCode == HttpStatusCode.OK;
                }
                catch
                {
                    return false;
                }
            }, TimeSpan.FromSeconds(10), TimeSpan.FromMilliseconds(200));

            started.Should().BeTrue("the server should accept /process after starting via EntryPoint --serve");
        }
        finally
        {
            if (!proc.HasExited)
            {
                try { proc.Kill(entireProcessTree: true); } catch { /* ignore */ }
            }
        }
    }

    [Fact]
    public async Task Main_Manual_Exits_Success_With_Overrides()
    {
        // Arrange stub services
        await using var extractor = await StartStubApp(map: app => app.MapPost("/extract", () => Results.Json(new { mentions = Array.Empty<object>() })));
        await using var notifier = await StartStubApp(map: app => app.MapPost("/", () => Results.Ok()));

        var solutionDir = FindSolutionDir();
        var projectPath = Path.Combine(solutionDir, "src", "DmeExtractorAgent");

        using var proc = new Process();
        proc.StartInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --project \"{projectPath}\" -- \"Doctor: Order CPAP\" --threshold 0.5 --nlp-url {extractor.BaseAddress} --notifications-url {notifier.BaseAddress}",
            WorkingDirectory = solutionDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        proc.StartInfo.Environment["DOTNET_ENVIRONMENT"] = "Production";

        proc.Start();
        await proc.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(20));
        proc.ExitCode.Should().Be(0);
    }

    private static async Task<bool> WaitUntilAsync(Func<Task<bool>> condition, TimeSpan timeout, TimeSpan pollInterval)
    {
        var start = DateTime.UtcNow;
        while (DateTime.UtcNow - start < timeout)
        {
            if (await condition()) return true;
            await Task.Delay(pollInterval);
        }
        return false;
    }

    private static int GetFreeTcpPort()
    {
        using var listener = new TcpListener(System.Net.IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private static string FindSolutionDir()
    {
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (dir != null)
        {
            var sln = Path.Combine(dir.FullName, "DmeExtractorAgent.sln");
            if (File.Exists(sln)) return dir.FullName;
            dir = dir.Parent;
        }
        throw new DirectoryNotFoundException("Could not locate solution directory containing DmeExtractorAgent.sln");
    }

    private sealed class StubApp : IAsyncDisposable
    {
        public required WebApplication App { get; init; }
        public required string BaseAddress { get; init; }
        public async ValueTask DisposeAsync()
        {
            await App.StopAsync();
            await App.DisposeAsync();
        }
    }

    private static async Task<StubApp> StartStubApp(Action<WebApplication> map)
    {
        var builder = WebApplication.CreateBuilder();
        var app = builder.Build();
        map(app);
        app.Urls.Add("http://127.0.0.1:0");
        await app.StartAsync();
        var address = app.Urls.First(u => u.StartsWith("http://"));
        return new StubApp { App = app, BaseAddress = address };
    }
}
