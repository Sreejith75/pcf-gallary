namespace AppWeaver.AIBrain.Generation;

/// <summary>
/// Represents the deterministic plan for generating PCF component files.
/// </summary>
public sealed class FileGenerationPlan
{
    /// <summary>
    /// Contract version (must be 1.0).
    /// </summary>
    public string Version { get; init; } = "1.0";

    /// <summary>
    /// The component type identifier (e.g., star-rating).
    /// </summary>
    public required string ComponentType { get; init; }

    /// <summary>
    /// Ordered list of steps to execute.
    /// </summary>
    public required IReadOnlyList<FileGenerationStep> Steps { get; init; }
}

/// <summary>
/// A single step in the file generation process.
/// </summary>
public sealed class FileGenerationStep
{
    /// <summary>
    /// Execution order (1-based).
    /// </summary>
    public int Order { get; init; }

    /// <summary>
    /// Name of the Handlebars template to use.
    /// </summary>
    public required string TemplateName { get; init; }

    /// <summary>
    /// Relative output path for the generated file.
    /// </summary>
    public required string OutputPath { get; init; }

    /// <summary>
    /// Whether this file is mandatory (always true in v1.0).
    /// </summary>
    public bool Required { get; init; }
}
