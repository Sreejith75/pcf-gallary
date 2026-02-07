using AppWeaver.AIBrain.Models.Specs;

namespace AppWeaver.AIBrain.Build;

/// <summary>
/// Result of a PCF component build.
/// </summary>
public sealed class BuildResult
{
    /// <summary>
    /// Unique identifier for this build.
    /// </summary>
    public required string BuildId { get; init; }

    /// <summary>
    /// Absolute path to the generated ZIP file.
    /// </summary>
    public required string ZipPath { get; init; }

    /// <summary>
    /// Whether the build succeeded.
    /// </summary>
    public required bool Success { get; init; }
}

/// <summary>
/// Orchestrates the deterministic build process from ComponentSpec to ZIP.
/// </summary>
public interface IBuildOrchestrator
{
    /// <summary>
    /// Builds a PCF component from a validated specification.
    /// </summary>
    /// <param name="spec">The validated component specification.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Build result containing ZIP path.</returns>
    /// <exception cref="BuildOrchestrationException">If any step fails.</exception>
    Task<BuildResult> BuildAsync(
        ComponentSpec spec,
        CancellationToken cancellationToken = default
    );
}

public class BuildOrchestrationException : Exception
{
    public BuildOrchestrationException(string message) : base(message) { }
    public BuildOrchestrationException(string message, Exception inner) : base(message, inner) { }
}
