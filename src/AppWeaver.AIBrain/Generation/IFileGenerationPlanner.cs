using AppWeaver.AIBrain.Models.Specs;

namespace AppWeaver.AIBrain.Generation;

/// <summary>
/// Interface for deterministically planning file generation.
/// </summary>
public interface IFileGenerationPlanner
{
    /// <summary>
    /// Creates a deterministic file generation plan for the given component spec.
    /// </summary>
    /// <param name="spec">The validated component specification.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A validated file generation plan.</returns>
    FileGenerationPlan CreatePlan(
        ComponentSpec spec,
        CancellationToken cancellationToken = default
    );
}
