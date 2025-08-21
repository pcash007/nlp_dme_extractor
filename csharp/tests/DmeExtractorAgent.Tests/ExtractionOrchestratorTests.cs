using DmeExtractorAgent;
using DmeExtractorAgent.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DmeExtractorAgent.Tests;

public class ExtractionOrchestratorTests
{
    [Fact]
    public async Task RunOnceAsync_Extracts_And_Posts()
    {
        var extractor = new Mock<INlpExtractorClient>(MockBehavior.Strict);
        var notifications = new Mock<INotificationClient>(MockBehavior.Strict);
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>{["Agent:Threshold"]="0.5"}).Build();
        var logger = Mock.Of<ILogger<ExtractionOrchestrator>>();

        var expected = new { ok = true };
        extractor.Setup(e => e.ExtractAsync(It.IsAny<string>(), It.IsAny<double>())).ReturnsAsync(expected);
        object? captured = null;
        notifications.Setup(n => n.PostAsync(It.IsAny<object>()))
            .Callback<object>(x => captured = x)
            .ReturnsAsync(true);

        var orchestrator = new ExtractionOrchestrator(extractor.Object, notifications.Object, config, logger);

        var ok = await orchestrator.RunOnceAsync("text");

        ok.Should().BeTrue();
    extractor.Verify(e => e.ExtractAsync(It.IsAny<string>(), 0.5), Times.Once);
    notifications.Verify(n => n.PostAsync(It.IsAny<object>()), Times.Once);
    captured.Should().NotBeNull();
    captured.Should().BeSameAs(expected);
    }
}
