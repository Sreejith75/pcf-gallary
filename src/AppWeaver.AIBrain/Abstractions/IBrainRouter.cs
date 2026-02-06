using AppWeaver.AIBrain.Models;

namespace AppWeaver.AIBrain.Abstractions;

/// <summary>
/// Routes brain tasks to the minimal required context.
/// Enforces the principle: "AI Brain is indexed, not injected."
/// </summary>
public interface IBrainRouter
{
    /// <summary>
    /// Routes a brain task to its required artifacts and produces a minimal context.
    /// </summary>
    /// <param name="task">The type of brain task to execute</param>
    /// <param name="parameters">Optional task-specific parameters (e.g., capability ID, schema name)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A minimal brain context containing only what is required for this task</returns>
    /// <exception cref="InvalidOperationException">If required artifacts cannot be loaded</exception>
    Task<IBrainContext> RouteAsync(
        BrainTask task,
        Dictionary<string, string>? parameters = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the list of files that will be loaded for a given task.
    /// Useful for logging, debugging, and token budget calculation.
    /// </summary>
    /// <param name="task">The type of brain task</param>
    /// <param name="parameters">Optional task-specific parameters</param>
    /// <returns>List of relative file paths that will be loaded</returns>
    IReadOnlyList<string> GetRequiredFiles(BrainTask task, Dictionary<string, string>? parameters = null);

    /// <summary>
    /// Estimates the token count for a given task's context.
    /// Used for budget enforcement before LLM calls.
    /// </summary>
    /// <param name="task">The type of brain task</param>
    /// <param name="parameters">Optional task-specific parameters</param>
    /// <returns>Estimated token count</returns>
    Task<int> EstimateTokenCountAsync(BrainTask task, Dictionary<string, string>? parameters = null);
}
