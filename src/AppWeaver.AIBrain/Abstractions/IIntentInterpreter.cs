using AppWeaver.AIBrain.Models.Intent;

namespace AppWeaver.AIBrain.Intent;

/// <summary>
/// Interface for intent interpretation.
/// This is the single authority for translating user text into GlobalIntent.
/// </summary>
public interface IIntentInterpreter
{
    /// <summary>
    /// Interprets raw user text into a validated GlobalIntent.
    /// </summary>
    /// <param name="rawUserText">The user's natural language input</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Intent interpretation result</returns>
    /// <exception cref="IntentInterpreterExecutionException">If Node.js execution fails</exception>
    /// <exception cref="IntentValidationException">If validation fails</exception>
    Task<IntentInterpretationResult> InterpretAsync(
        string rawUserText,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of intent interpretation.
/// </summary>
public sealed class IntentInterpretationResult
{
    /// <summary>
    /// The validated GlobalIntent.
    /// NULL if needsClarification is true.
    /// </summary>
    public GlobalIntent? Intent { get; init; }

    /// <summary>
    /// Confidence score from LLM (0.0 - 1.0).
    /// </summary>
    public double Confidence { get; init; }

    /// <summary>
    /// Phrases that could not be mapped to the schema.
    /// </summary>
    public required IReadOnlyList<string> UnmappedPhrases { get; init; }

    /// <summary>
    /// Whether clarification is needed from the user.
    /// TRUE if confidence &lt; 0.6 or intent is ambiguous.
    /// </summary>
    public bool NeedsClarification { get; init; }
}
