using System.Net;
using System.Net.Http.Json;
using DmeExtractorAgent.Web;
using DmeExtractorAgent.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;
using FluentAssertions;

namespace DmeExtractorAgent.Tests;

public class HttpServerIntegrationTests
{
    [Fact]
    public async Task Process_Posts_Through_Orchestrator()
    {
        // Arrange: build app with in-memory configs
        var args = Array.Empty<string>();
        var app = HttpServer.Build(args);

        // Use TestServer via WebApplicationFactory pattern: run Kestrel on random port
        var url = "http://127.0.0.1:0"; // dynamic
        var tcs = new TaskCompletionSource<int>();
        var runTask = Task.Run(async () =>
        {
            app.Urls.Add(url);
            await app.StartAsync();
            tcs.TrySetResult(0);
            await app.WaitForShutdownAsync();
        });
        await tcs.Task; // ensure started

        try
        {
            // Discover the bound address
            var address = app.Urls.First(u => u.StartsWith("http://"));
            using var http = new HttpClient();
            var resp = await http.PostAsJsonAsync(new Uri(new Uri(address), "/process"), new Request("some text"));
            resp.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await resp.Content.ReadFromJsonAsync<Dictionary<string, object>>();
            body.Should().NotBeNull();
            body!.Should().ContainKey("posted");
        }
        finally
        {
            await app.StopAsync();
            await app.DisposeAsync();
        }
    }
}
