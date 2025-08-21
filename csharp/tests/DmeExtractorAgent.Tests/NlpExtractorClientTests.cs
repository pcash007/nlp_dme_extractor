using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using DmeExtractorAgent;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RichardSzalay.MockHttp;
using Xunit;

namespace DmeExtractorAgent.Tests;

public class NlpExtractorClientTests
{
    [Fact]
    public async Task ExtractAsync_Returns_Object_When_200()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Post, "http://localhost:8000/extract")
                .Respond("application/json", "{ \"mentions\": [] }");

        var http = mockHttp.ToHttpClient();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Agent:NlpExtractorUrl"] = "http://localhost:8000"
            })
            .Build();
        using var loggerFactory = LoggerFactory.Create(b => b.AddConsole());
        var logger = loggerFactory.CreateLogger<NlpExtractorClient>();

        var client = new NlpExtractorClient(http, config, logger);

        // Act
        var result = await client.ExtractAsync("hello", 0.45);

        // Assert
        result.Should().NotBeNull();
    }
}
