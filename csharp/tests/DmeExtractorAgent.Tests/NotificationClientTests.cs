using System.Net.Http;
using System.Net.Http.Json;
using DmeExtractorAgent;
using DmeExtractorAgent.Services.Http;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RichardSzalay.MockHttp;
using Xunit;

namespace DmeExtractorAgent.Tests;

public class NotificationClientTests
{
    [Fact]
    public async Task PostAsync_True_On_200()
    {
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Post, "https://example.com/*")
                .Respond("application/json", "{ } ");

        var http = mockHttp.ToHttpClient();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Agent:NotificationUrl"] = "https://example.com"
            })
            .Build();
        using var loggerFactory = LoggerFactory.Create(b => b.AddConsole());
        var logger = loggerFactory.CreateLogger<NotificationClient>();

        var client = new NotificationClient(http, config, logger);

        var ok = await client.PostAsync(new { hello = "world" });
        ok.Should().BeTrue();
    }
}
