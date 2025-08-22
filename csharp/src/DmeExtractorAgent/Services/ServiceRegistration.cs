using Microsoft.Extensions.DependencyInjection;
using DmeExtractorAgent;
using DmeExtractorAgent.Services;
using DmeExtractorAgent.Services.Http;

namespace DmeExtractorAgent.Services;

public static class ServiceRegistration
{
    // Registers orchestrator and typed HttpClients for reuse in web and non-web hosts
    public static IServiceCollection AddAgentCoreServices(this IServiceCollection services)
    {
    services.AddTransient<ExtractionOrchestrator>();
    services.AddHttpClient<INlpExtractorClient, NlpExtractorClient>();
    services.AddHttpClient<INotificationClient, NotificationClient>();
        return services;
    }
}
