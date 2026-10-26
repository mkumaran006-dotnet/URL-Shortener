using Microsoft.Extensions.DependencyInjection;
using UrlShortener.Orchestration.Engine;
using UrlShortener.Orchestration.Governance;
using UrlShortener.Orchestration.Reliability;
using UrlShortener.Orchestration.Scenarios;
using UrlShortener.Orchestration.Stages;

namespace UrlShortener.Orchestration.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOrchestration(this IServiceCollection services)
    {
        // Stages
        services.AddSingleton<IStage, RequirementsStage>();
        services.AddSingleton<IStage, ArchitectureStage>();
        services.AddSingleton<IStage, ImplementationStage>();
        services.AddSingleton<IStage, TestingStage>();
        services.AddSingleton<IStage, DocumentationStage>();
        services.AddSingleton<IStage, ReleaseReadinessStage>();

        // Governance
        services.AddSingleton<IAuditLogger, AuditLogger>();
        services.AddSingleton<IApprovalService, AutoApprovalService>();
        services.AddSingleton(_ => PolicyGuardrailService.CreateDefault());

        // Reliability
        services.AddSingleton<RollbackManager>();
        services.AddSingleton<MetricsCollector>();

        // Engine
        services.AddSingleton<IOrchestrationEngine, OrchestrationEngine>();

        // Scenarios
        services.AddTransient<GreenfieldScenario>();
        services.AddTransient<BrownfieldScenario>();
        services.AddTransient<AmbiguousScenario>();

        return services;
    }
}
