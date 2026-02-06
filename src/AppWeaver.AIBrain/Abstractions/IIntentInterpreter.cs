using AppWeaver.AIBrain.Models.Intent;
using AppWeaver.AIBrain.Models.Capabilities;

namespace AppWeaver.AIBrain.Abstractions;

/// <summary>
/// Interprets natural language prompts into structured GlobalIntent.
/// This is a pure validation and mapping layer - no LLM calls.
/// </summary>
public interface IIntentInterpreter
{
    /// <summary>
    /// Validates a GlobalIntent object against the schema and intent rules.
    /// </summary>
    /// <param name="intent">The intent to validate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result with any errors or warnings</returns>
    Task<IntentValidationResult> ValidateIntentAsync(
        GlobalIntent intent,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a GlobalIntent is ambiguous and requires clarification.
    /// </summary>
    /// <param name="intent">The intent to check</param>
    /// <returns>Ambiguity result with clarification questions if needed</returns>
    Task<AmbiguityCheckResult> CheckAmbiguityAsync(GlobalIntent intent);
}

/// <summary>
/// Result of intent validation.
/// </summary>
public record IntentValidationResult
{
    /// <summary>
    /// Whether the intent is valid.
    /// </summary>
    public required bool IsValid { get; init; }

    /// <summary>
    /// Validation errors, if any.
    /// </summary>
    public required IReadOnlyList<string> Errors { get; init; }

    /// <summary>
    /// Validation warnings, if any.
    /// </summary>
    public required IReadOnlyList<string> Warnings { get; init; }
}

/// <summary>
/// Result of ambiguity checking.
/// </summary>
public record AmbiguityCheckResult
{
    /// <summary>
    /// Whether the intent is ambiguous.
    /// </summary>
    public required bool IsAmbiguous { get; init; }

    /// <summary>
    /// Clarification question to ask the user, if ambiguous.
    /// </summary>
    public string? ClarificationNeeded { get; init; }

    /// <summary>
    /// Possible options for the user to choose from.
    /// </summary>
    public IReadOnlyList<string>? Options { get; init; }
}
