namespace AppWeaver.AIBrain.Api.Models;

/// <summary>
/// Response for a component build request.
/// </summary>
public class ComponentBuildResponse
{
    /// <summary>
    /// Unique identifier for the build.
    /// </summary>
    public string BuildId { get; set; } = string.Empty;

    /// <summary>
    /// Current status of the build.
    /// </summary>
    /// <example>Running</example>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// URL to download the resulting ZIP artifact.
    /// </summary>
    public string ZipDownloadUrl { get; set; } = string.Empty;
}
