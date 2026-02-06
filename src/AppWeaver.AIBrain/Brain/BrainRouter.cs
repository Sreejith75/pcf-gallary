using AppWeaver.AIBrain.Abstractions;
using AppWeaver.AIBrain.Configuration;
using AppWeaver.AIBrain.Models;
using AppWeaver.AIBrain.Models.Capabilities;
using Microsoft.Extensions.Options;


namespace AppWeaver.AIBrain.Brain;

/// <summary>
/// Routes brain tasks to the minimal required context.
/// Implements deterministic, explicit routing logic.
/// </summary>
public class BrainRouter : IBrainRouter
{
    private readonly IBrainLoader _loader;
    private readonly BrainOptions _options;
    private readonly Dictionary<string, (DateTimeOffset LoadedAt, object Data)> _cache = new();

    public BrainRouter(IBrainLoader loader, IOptions<BrainOptions> options)
    {
        _loader = loader;
        _options = options.Value;
    }

    /// <inheritdoc />
    public async Task<IBrainContext> RouteAsync(
        BrainTask task,
        Dictionary<string, string>? parameters = null,
        CancellationToken cancellationToken = default)
    {
        parameters ??= new Dictionary<string, string>();
        var filesLoaded = new List<string>();
        var cacheHit = false;

        var metadata = new BrainContextMetadata
        {
            Version = BrainContracts.Version,
            FilesLoaded = filesLoaded,
            EstimatedTokens = 0,
            CreatedAt = DateTimeOffset.UtcNow,
            CacheHit = cacheHit
        };

        var context = new BrainContext(task, metadata);

        // Route based on task type
        switch (task)
        {
            case BrainTask.InterpretIntent:
                await LoadIntentInterpretationArtifactsAsync(context, filesLoaded, cancellationToken);
                break;

            case BrainTask.MatchCapability:
                await LoadCapabilityMatchingArtifactsAsync(context, filesLoaded, parameters, cancellationToken);
                break;

            case BrainTask.GenerateComponentSpec:
                await LoadSpecGenerationArtifactsAsync(context, filesLoaded, parameters, cancellationToken);
                break;

            case BrainTask.ValidateRules:
                await LoadRulesValidationArtifactsAsync(context, filesLoaded, cancellationToken);
                break;

            case BrainTask.ValidateFinal:
                await LoadFinalValidationArtifactsAsync(context, filesLoaded, parameters, cancellationToken);
                break;

            case BrainTask.LoadSchema:
                await LoadSchemaArtifactAsync(context, filesLoaded, parameters, cancellationToken);
                break;

            case BrainTask.LoadCapability:
                await LoadCapabilityArtifactAsync(context, filesLoaded, parameters, cancellationToken);
                break;

            case BrainTask.LoadPrompt:
                await LoadPromptArtifactAsync(context, filesLoaded, parameters, cancellationToken);
                break;

            default:
                throw new ArgumentException($"Unknown brain task: {task}");
        }

        // Estimate tokens
        metadata = metadata with { EstimatedTokens = EstimateTokens(filesLoaded) };

        return context;
    }

    /// <inheritdoc />
    public IReadOnlyList<string> GetRequiredFiles(BrainTask task, Dictionary<string, string>? parameters = null)
    {
        parameters ??= new Dictionary<string, string>();

        return task switch
        {
            BrainTask.InterpretIntent => new[]
            {
                "schemas/global-intent.schema.json",
                "intent/intent-mapping.rules.json",
                "intent/ambiguity-resolution.rules.json"
            },
            BrainTask.MatchCapability => new[]
            {
                "capabilities/registry.index.json",
                $"capabilities/{parameters.GetValueOrDefault("capabilityId", "star-rating")}.capability.json"
            },
            BrainTask.GenerateComponentSpec => new[]
            {
                "schemas/component-spec.schema.json",
                $"capabilities/{parameters.GetValueOrDefault("capabilityId", "star-rating")}.capability.json"
            },
            BrainTask.ValidateRules => new[]
            {
                "rules/pcf-core.rules.md",
                "rules/pcf-accessibility.rules.md"
            },
            BrainTask.ValidateFinal => new[]
            {
                "schemas/component-spec.schema.json",
                $"capabilities/{parameters.GetValueOrDefault("capabilityId", "star-rating")}.capability.json"
            },
            BrainTask.LoadSchema => new[]
            {
                $"schemas/{parameters.GetValueOrDefault("schemaName", "global-intent.schema.json")}"
            },
            BrainTask.LoadCapability => new[]
            {
                $"capabilities/{parameters.GetValueOrDefault("capabilityId", "star-rating")}.capability.json"
            },
            BrainTask.LoadPrompt => new[]
            {
                $"prompts/{parameters.GetValueOrDefault("promptName", "intent-interpreter.prompt.md")}"
            },
            _ => Array.Empty<string>()
        };
    }

    /// <inheritdoc />
    public async Task<int> EstimateTokenCountAsync(BrainTask task, Dictionary<string, string>? parameters = null)
    {
        var files = GetRequiredFiles(task, parameters);
        return EstimateTokens(files);
    }

    private async Task LoadIntentInterpretationArtifactsAsync(
        BrainContext context,
        List<string> filesLoaded,
        CancellationToken cancellationToken)
    {
        // Load schema
        var schema = await LoadWithCacheAsync<object>(
            "schemas/global-intent.schema.json",
            filesLoaded,
            cancellationToken);
        context.AddArtifact("schema", schema);

        // Load intent mapping rules
        var intentRules = await LoadWithCacheAsync<object>(
            "intent/intent-mapping.rules.json",
            filesLoaded,
            cancellationToken);
        context.AddArtifact("intentMappingRules", intentRules);

        // Load ambiguity resolution rules
        var ambiguityRules = await LoadWithCacheAsync<object>(
            "intent/ambiguity-resolution.rules.json",
            filesLoaded,
            cancellationToken);
        context.AddArtifact("ambiguityResolutionRules", ambiguityRules);
    }

    private async Task LoadCapabilityMatchingArtifactsAsync(
        BrainContext context,
        List<string> filesLoaded,
        Dictionary<string, string> parameters,
        CancellationToken cancellationToken)
    {
        // Load registry
        var registry = await LoadWithCacheAsync<object>(
            "capabilities/registry.index.json",
            filesLoaded,
            cancellationToken);
        context.AddArtifact("registry", registry);

        // Load specific capability if provided
        if (parameters.TryGetValue("capabilityId", out var capabilityId))
        {
            var capability = await LoadWithCacheAsync<ComponentCapability>(
                $"capabilities/{capabilityId}.capability.json",
                filesLoaded,
                cancellationToken);
            context.AddArtifact("capability", capability);
        }
    }

    private async Task LoadSpecGenerationArtifactsAsync(
        BrainContext context,
        List<string> filesLoaded,
        Dictionary<string, string> parameters,
        CancellationToken cancellationToken)
    {
        // Load component spec schema
        var schema = await LoadWithCacheAsync<object>(
            "schemas/component-spec.schema.json",
            filesLoaded,
            cancellationToken);
        context.AddArtifact("schema", schema);

        // Load capability
        var capabilityId = parameters.GetValueOrDefault("capabilityId", "star-rating");
        var capability = await LoadWithCacheAsync<ComponentCapability>(
            $"capabilities/{capabilityId}.capability.json",
            filesLoaded,
            cancellationToken);
        context.AddArtifact("capability", capability);
    }

    private async Task LoadRulesValidationArtifactsAsync(
        BrainContext context,
        List<string> filesLoaded,
        CancellationToken cancellationToken)
    {
        // Load PCF core rules
        var coreRules = await _loader.LoadMarkdownAsync("rules/pcf-core.rules.md", cancellationToken);
        filesLoaded.Add("rules/pcf-core.rules.md");
        context.AddArtifact("coreRules", coreRules);

        // Load accessibility rules
        var a11yRules = await _loader.LoadMarkdownAsync("rules/pcf-accessibility.rules.md", cancellationToken);
        filesLoaded.Add("rules/pcf-accessibility.rules.md");
        context.AddArtifact("accessibilityRules", a11yRules);
    }

    private async Task LoadFinalValidationArtifactsAsync(
        BrainContext context,
        List<string> filesLoaded,
        Dictionary<string, string> parameters,
        CancellationToken cancellationToken)
    {
        // Load schema
        var schema = await LoadWithCacheAsync<object>(
            "schemas/component-spec.schema.json",
            filesLoaded,
            cancellationToken);
        context.AddArtifact("schema", schema);

        // Load capability for bounds checking
        var capabilityId = parameters.GetValueOrDefault("capabilityId", "star-rating");
        var capability = await LoadWithCacheAsync<ComponentCapability>(
            $"capabilities/{capabilityId}.capability.json",
            filesLoaded,
            cancellationToken);
        context.AddArtifact("capability", capability);
    }

    private async Task LoadSchemaArtifactAsync(
        BrainContext context,
        List<string> filesLoaded,
        Dictionary<string, string> parameters,
        CancellationToken cancellationToken)
    {
        var schemaName = parameters.GetValueOrDefault("schemaName", "global-intent.schema.json");
        var schema = await LoadWithCacheAsync<object>(
            $"schemas/{schemaName}",
            filesLoaded,
            cancellationToken);
        context.AddArtifact("schema", schema);
    }

    private async Task LoadCapabilityArtifactAsync(
        BrainContext context,
        List<string> filesLoaded,
        Dictionary<string, string> parameters,
        CancellationToken cancellationToken)
    {
        var capabilityId = parameters.GetValueOrDefault("capabilityId", "star-rating");
        var capability = await LoadWithCacheAsync<ComponentCapability>(
            $"capabilities/{capabilityId}.capability.json",
            filesLoaded,
            cancellationToken);
        context.AddArtifact("capability", capability);
    }

    private async Task LoadPromptArtifactAsync(
        BrainContext context,
        List<string> filesLoaded,
        Dictionary<string, string> parameters,
        CancellationToken cancellationToken)
    {
        var promptName = parameters.GetValueOrDefault("promptName", "intent-interpreter.prompt.md");
        var prompt = await _loader.LoadMarkdownAsync($"prompts/{promptName}", cancellationToken);
        filesLoaded.Add($"prompts/{promptName}");
        context.AddArtifact("prompt", prompt);
    }

    private async Task<T> LoadWithCacheAsync<T>(
        string relativePath,
        List<string> filesLoaded,
        CancellationToken cancellationToken) where T : class
    {
        filesLoaded.Add(relativePath);

        if (_options.EnableCaching && _cache.TryGetValue(relativePath, out var cached))
        {
            var age = DateTimeOffset.UtcNow - cached.LoadedAt;
            if (age.TotalMinutes < _options.CacheExpirationMinutes)
            {
                return (T)cached.Data;
            }
            _cache.Remove(relativePath);
        }

        var data = await _loader.LoadJsonAsync<T>(relativePath, cancellationToken);

        if (_options.EnableCaching)
        {
            _cache[relativePath] = (DateTimeOffset.UtcNow, data);
        }

        return data;
    }

    private int EstimateTokens(IReadOnlyList<string> files)
    {
        // Rough estimation: 1 token ≈ 4 characters
        // Average file size estimates based on actual brain files
        var estimates = new Dictionary<string, int>
        {
            ["schemas/global-intent.schema.json"] = 1200,
            ["schemas/component-spec.schema.json"] = 1500,
            ["intent/intent-mapping.rules.json"] = 800,
            ["intent/ambiguity-resolution.rules.json"] = 900,
            ["capabilities/registry.index.json"] = 500,
            ["capabilities/star-rating.capability.json"] = 1500,
            ["rules/pcf-core.rules.md"] = 1500,
            ["rules/pcf-accessibility.rules.md"] = 1000
        };

        return files.Sum(f => estimates.GetValueOrDefault(f, 500));
    }
}
