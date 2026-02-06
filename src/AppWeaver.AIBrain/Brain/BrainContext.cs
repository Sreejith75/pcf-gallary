using AppWeaver.AIBrain.Abstractions;
using AppWeaver.AIBrain.Models;
using System.Text.Json;

namespace AppWeaver.AIBrain.Brain;

/// <summary>
/// Represents the minimal context required for a specific brain task.
/// This is the ONLY object that should be passed to LLM execution layers.
/// </summary>
public class BrainContext : IBrainContext
{
    private readonly Dictionary<string, object> _artifacts = new();
    private readonly BrainContextMetadata _metadata;

    public BrainContext(
        BrainTask task,
        BrainContextMetadata metadata)
    {
        Task = task;
        _metadata = metadata;
    }

    /// <inheritdoc />
    public BrainTask Task { get; }

    /// <inheritdoc />
    public BrainContextMetadata Metadata => _metadata;

    /// <summary>
    /// Adds an artifact to the context.
    /// </summary>
    public void AddArtifact(string key, object artifact)
    {
        _artifacts[key] = artifact;
    }

    /// <inheritdoc />
    public T? GetArtifact<T>(string key) where T : class
    {
        if (_artifacts.TryGetValue(key, out var artifact))
        {
            return artifact as T;
        }
        return null;
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, object> GetAllArtifacts()
    {
        return _artifacts;
    }

    /// <inheritdoc />
    public string ToJson(JsonSerializerOptions? options = null)
    {
        options ??= new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var contextData = new
        {
            task = Task.ToString(),
            metadata = new
            {
                filesLoaded = Metadata.FilesLoaded,
                estimatedTokens = Metadata.EstimatedTokens,
                createdAt = Metadata.CreatedAt,
                cacheHit = Metadata.CacheHit
            },
            artifacts = _artifacts
        };

        return JsonSerializer.Serialize(contextData, options);
    }
}
