namespace AppWeaver.AIBrain.Configuration;

/// <summary>
/// Configuration options for the AI Brain runtime.
/// </summary>
public class BrainOptions
{
    /// <summary>
    /// Absolute path to the ai-brain directory.
    /// </summary>
    public required string BrainRootPath { get; set; }

    /// <summary>
    /// Maximum token budget per brain task (default: 5000).
    /// </summary>
    public int MaxTokensPerTask { get; set; } = 5000;

    /// <summary>
    /// Maximum number of files to load per task (default: 10).
    /// </summary>
    public int MaxFilesPerTask { get; set; } = 10;

    /// <summary>
    /// Whether to enable in-memory caching of brain artifacts.
    /// </summary>
    public bool EnableCaching { get; set; } = true;

    /// <summary>
    /// Cache expiration in minutes (default: 60).
    /// </summary>
    public int CacheExpirationMinutes { get; set; } = 60;

    /// <summary>
    /// Whether to validate brain artifacts on load (recommended: true).
    /// </summary>
    public bool ValidateOnLoad { get; set; } = true;

    /// <summary>
    /// Default namespace for generated components.
    /// </summary>
    public string DefaultNamespace { get; set; } = "Contoso";
}
