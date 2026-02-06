using AppWeaver.AIBrain.Models.Intent;
using AppWeaver.AIBrain.Models.Specs;

namespace AppWeaver.AIBrain.Abstractions;

/// <summary>
/// Executes deterministic procedures defined in the AI Brain.
/// Think: "If I remove AI completely, this still behaves correctly."
/// </summary>
public interface IProcedureExecutor
{
    /// <summary>
    /// Executes the create-component procedure.
    /// This orchestrates the full pipeline but does NOT call LLMs directly.
    /// </summary>
    /// <param name="userPrompt">The natural language prompt from the user</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Execution plan that the Node.js layer will execute</returns>
    Task<ComponentExecutionPlan> ExecuteCreateComponentAsync(
        string userPrompt,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a ComponentSpec against all rules.
    /// </summary>
    /// <param name="spec">The component specification to validate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result with errors, warnings, and downgrades</returns>
    Task<SpecValidationResult> ValidateComponentSpecAsync(
        ComponentSpec spec,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Execution plan produced by the C# brain for the Node.js layer to execute.
/// </summary>
public record ComponentExecutionPlan
{
    /// <summary>
    /// Contract version for C# ↔ Node.js communication.
    /// </summary>
    public required string Version { get; init; } = BrainContracts.Version;

    /// <summary>
    /// Unique build ID for this execution.
    /// </summary>
    public required string BuildId { get; init; }

    /// <summary>
    /// The validated GlobalIntent.
    /// </summary>
    public required GlobalIntent Intent { get; init; }

    /// <summary>
    /// The matched capability.
    /// </summary>
    public required string CapabilityId { get; init; }

    /// <summary>
    /// The approved ComponentSpec (after all validation).
    /// </summary>
    public required ComponentSpec ComponentSpec { get; init; }

    /// <summary>
    /// Files to generate (in order).
    /// </summary>
    public required IReadOnlyList<FileGenerationStep> FilesToGenerate { get; init; }

    /// <summary>
    /// Validation report.
    /// </summary>
    public required SpecValidationResult ValidationReport { get; init; }
}

/// <summary>
/// A single file generation step.
/// </summary>
public record FileGenerationStep
{
    /// <summary>
    /// Step number (1-indexed).
    /// </summary>
    public required int Step { get; init; }

    /// <summary>
    /// File name to generate.
    /// </summary>
    public required string FileName { get; init; }

    /// <summary>
    /// Output path relative to project root.
    /// </summary>
    public required string OutputPath { get; init; }

    /// <summary>
    /// Template path to use.
    /// </summary>
    public required string TemplatePath { get; init; }

    /// <summary>
    /// Validation type to apply after generation.
    /// </summary>
    public required string ValidationType { get; init; }

    /// <summary>
    /// Whether this file is required.
    /// </summary>
    public required bool Required { get; init; }
}

/// <summary>
/// Result of component spec validation.
/// </summary>
public record SpecValidationResult
{
    /// <summary>
    /// Contract version for C# ↔ Node.js communication.
    /// </summary>
    public required string Version { get; init; } = BrainContracts.Version;

    /// <summary>
    /// Whether the spec is valid.
    /// </summary>
    public required bool IsValid { get; init; }

    /// <summary>
    /// Validation errors (will cause rejection).
    /// </summary>
    public required IReadOnlyList<ValidationError> Errors { get; init; }

    /// <summary>
    /// Validation warnings (logged but not blocking).
    /// </summary>
    public required IReadOnlyList<ValidationWarning> Warnings { get; init; }

    /// <summary>
    /// Downgrades applied (auto-fixes).
    /// </summary>
    public required IReadOnlyList<ValidationDowngrade> Downgrades { get; init; }

    /// <summary>
    /// Total rules executed.
    /// </summary>
    public required int TotalRules { get; init; }

    /// <summary>
    /// Rules that passed.
    /// </summary>
    public required int PassedRules { get; init; }
}

/// <summary>
/// A validation error.
/// </summary>
public record ValidationError
{
    public required string RuleId { get; init; }
    public required string Message { get; init; }
    public string? Suggestion { get; init; }
    public required bool AutoFixable { get; init; }
}

/// <summary>
/// A validation warning.
/// </summary>
public record ValidationWarning
{
    public required string RuleId { get; init; }
    public required string Message { get; init; }
    public string? Suggestion { get; init; }
}

/// <summary>
/// A validation downgrade (auto-fix applied).
/// </summary>
public record ValidationDowngrade
{
    public required string RuleId { get; init; }
    public required string Message { get; init; }
    public required string AutoFix { get; init; }
}
