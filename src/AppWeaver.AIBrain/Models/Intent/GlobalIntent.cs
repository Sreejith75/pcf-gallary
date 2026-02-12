using System.Text.Json.Serialization;

namespace AppWeaver.AIBrain.Models.Intent;

/// <summary>
/// Represents a structured interpretation of user's natural language prompt.
/// Maps to ai-brain/schemas/global-intent.schema.json
/// </summary>
public record GlobalIntent
{
    /// <summary>
    /// Component classification (e.g., "input-control", "display-control").
    /// </summary>
    [JsonPropertyName("classification")]
    public required string Classification { get; init; }

    /// <summary>
    /// Specific component type identifier (e.g., "star-rating", "text-input").
    /// </summary>
    [JsonPropertyName("componentType")]
    public required string ComponentType { get; init; }

    /// <summary>
    /// UI intent details.
    /// </summary>
    [JsonPropertyName("uiIntent")]
    public required UiIntent UiIntent { get; init; }

    /// <summary>
    /// Behavior specifications.
    /// </summary>
    [JsonPropertyName("behavior")]
    public required Behavior Behavior { get; init; }

    /// <summary>
    /// Interaction specifications.
    /// </summary>
    [JsonPropertyName("interaction")]
    public required Interaction Interaction { get; init; }

    /// <summary>
    /// Accessibility requirements.
    /// </summary>
    [JsonPropertyName("accessibility")]
    public required Accessibility Accessibility { get; init; }

    /// <summary>
    /// Responsiveness requirements.
    /// </summary>
    [JsonPropertyName("responsiveness")]
    public required Responsiveness Responsiveness { get; init; }

    /// <summary>
    /// Constraints and limitations.
    /// </summary>
    [JsonPropertyName("constraints")]
    public required Constraints Constraints { get; init; }
}

public record UiIntent
{
    [JsonPropertyName("primaryPurpose")]
    public required string PrimaryPurpose { get; init; }

    [JsonPropertyName("visualStyle")]
    public required string VisualStyle { get; init; }

    [JsonPropertyName("dataBinding")]
    public required string DataBinding { get; init; }
}

public record Behavior
{
    [JsonPropertyName("interactivity")]
    public required string Interactivity { get; init; }

    [JsonPropertyName("validation")]
    public required string Validation { get; init; }

    [JsonPropertyName("persistence")]
    public required string Persistence { get; init; }
}

public record Interaction
{
    [JsonPropertyName("inputMethod")]
    public required List<string> InputMethod { get; init; }

    [JsonPropertyName("feedback")]
    public required List<string> Feedback { get; init; }
}

public record Accessibility
{
    [JsonPropertyName("wcagLevel")]
    public required string WcagLevel { get; init; }

    [JsonPropertyName("keyboardNavigable")]
    public required bool KeyboardNavigable { get; init; }

    [JsonPropertyName("screenReaderSupport")]
    public required bool ScreenReaderSupport { get; init; }

    [JsonPropertyName("highContrastMode")]
    public required bool HighContrastMode { get; init; }
}

public record Responsiveness
{
    [JsonPropertyName("adaptiveLayout")]
    public required bool AdaptiveLayout { get; init; }
}

public record Constraints
{
    [JsonPropertyName("performanceTarget")]
    public required string PerformanceTarget { get; init; }

    [JsonPropertyName("offlineCapable")]
    public required bool OfflineCapable { get; init; }

    [JsonPropertyName("externalDependencies")]
    public required List<string> ExternalDependencies { get; init; }
}
