namespace AppWeaver.AIBrain.Api.Models;

/// <summary>
/// Response containing the status of a build.
/// </summary>
public class BuildStatusResponse
{
    /// <summary>
    /// Unique identifier for the build.
    /// </summary>
    public string BuildId { get; set; } = string.Empty;

    /// <summary>
    /// Current status of the build (e.g., Running, Completed, Failed).
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Error message if the build failed.
    /// </summary>
    public string? Error { get; set; }
}
