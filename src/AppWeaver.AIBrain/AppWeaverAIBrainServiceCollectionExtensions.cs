using AppWeaver.AIBrain.Abstractions;
using AppWeaver.AIBrain.Brain;
using AppWeaver.AIBrain.Configuration;
using AppWeaver.AIBrain.Procedures;
using AppWeaver.AIBrain.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace AppWeaver.AIBrain;

/// <summary>
/// Extension methods for registering AppWeaver.AIBrain services.
/// </summary>
public static class AppWeaverAIBrainServiceCollectionExtensions
{
    /// <summary>
    /// Adds AppWeaver AI Brain services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="brainRootPath">Absolute path to the ai-brain directory</param>
    /// <param name="configureOptions">Optional configuration action</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddAppWeaverAIBrain(
        this IServiceCollection services,
        string brainRootPath,
        Action<BrainOptions>? configureOptions = null)
    {
        // Configure options
        services.Configure<BrainOptions>(options =>
        {
            options.BrainRootPath = brainRootPath;
            configureOptions?.Invoke(options);
        });

        // Register core services
        services.AddSingleton<IBrainLoader, BrainLoader>();
        services.AddSingleton<IBrainRouter, BrainRouter>();
        
        // Register validators
        services.AddSingleton<CapabilityValidator>();
        services.AddSingleton<RuleValidator>();

        // Register procedures
        services.AddScoped<IProcedureExecutor, CreateComponentProcedure>();

        // Register intent interpreter
        services.AddScoped<Intent.IIntentInterpreter, Intent.IntentInterpreter>();

        return services;
    }

    /// <summary>
    /// Adds AppWeaver AI Brain services with options configuration.
    /// </summary>
    public static IServiceCollection AddAppWeaverAIBrain(
        this IServiceCollection services,
        Action<BrainOptions> configureOptions)
    {
        services.Configure(configureOptions);

        // Register core services
        services.AddSingleton<IBrainLoader, BrainLoader>();
        services.AddSingleton<IBrainRouter, BrainRouter>();
        
        // Register validators
        services.AddSingleton<CapabilityValidator>();
        services.AddSingleton<RuleValidator>();

        // Register procedures
        services.AddScoped<IProcedureExecutor, CreateComponentProcedure>();

        return services;
    }
}
