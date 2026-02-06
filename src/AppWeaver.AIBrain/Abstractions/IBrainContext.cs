using AppWeaver.AIBrain.Models;
using System.Text.Json;

namespace AppWeaver.AIBrain.Abstractions;

/// <summary>
/// Represents the minimal context required for a specific brain task.
/// This is the ONLY object that should be passed to LLM execution layers.
/// Contains no file paths, no raw brain folders, only required data.
/// </summary>
public interface IBrainContext
{
    /// <summary>
    /// The task this context was created for.
    /// </summary>
    BrainTask Task { get; }

    /// <summary>
    /// Gets a typed artifact from the context.
    /// </summary>
    /// <typeparam name="T">The type of artifact to retrieve</typeparam>
    /// <param name="key">The artifact key (e.g., "schema", "capability", "rules")</param>
    /// <returns>The artifact if present, null otherwise</returns>
    T? GetArtifact<T>(string key) where T : class;

    /// <summary>
    /// Gets all artifacts in the context.
    /// </summary>
    /// <returns>Dictionary of all artifacts keyed by name</returns>
    IReadOnlyDictionary<string, object> GetAllArtifacts();

    /// <summary>
    /// Serializes the context to JSON for LLM consumption.
    /// </summary>
    /// <param name="options">JSON serialization options</param>
    /// <returns>JSON string representation of the context</returns>
    string ToJson(JsonSerializerOptions? options = null);

    /// <summary>
    /// Gets metadata about the context (files loaded, token estimate, etc.).
    /// </summary>
    BrainContextMetadata Metadata { get; }
}

/// <summary>
/// Metadata about a brain context.
/// </summary>
public record BrainContextMetadata
{
    /// <summary>
    /// Contract version for C# ↔ Node.js communication.
    /// </summary>
    public required string Version { get; init; } = BrainContracts.Version;

    /// <summary>
    /// Files that were loaded to create this context.
    /// </summary>
    public required IReadOnlyList<string> FilesLoaded { get; init; }

    /// <summary>
    /// Estimated token count for this context.
    /// </summary>
    public required int EstimatedTokens { get; init; }

    /// <summary>
    /// Timestamp when the context was created.
    /// </summary>
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// Whether any files were loaded from cache.
    /// </summary>
    public required bool CacheHit { get; init; }
}
