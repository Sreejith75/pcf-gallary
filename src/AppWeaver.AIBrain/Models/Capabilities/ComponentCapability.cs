using System.Text.Json.Serialization;

namespace AppWeaver.AIBrain.Models.Capabilities;

/// <summary>
/// Represents a PCF component capability definition.
/// Maps to ai-brain/capabilities/*.capability.json
/// </summary>
public record ComponentCapability
{
    /// <summary>
    /// Unique capability identifier (e.g., "star-rating").
    /// </summary>
    [JsonPropertyName("capabilityId")]
    public required string CapabilityId { get; init; }

    /// <summary>
    /// Display name for the capability.
    /// </summary>
    [JsonPropertyName("displayName")]
    public required string DisplayName { get; init; }

    /// <summary>
    /// Description of what this capability provides.
    /// </summary>
    [JsonPropertyName("description")]
    public required string Description { get; init; }

    /// <summary>
    /// Component classification.
    /// </summary>
    [JsonPropertyName("classification")]
    public required string Classification { get; init; }

    /// <summary>
    /// Primary purpose of this capability.
    /// </summary>
    [JsonPropertyName("primaryPurpose")]
    public required string PrimaryPurpose { get; init; }

    /// <summary>
    /// Supported features.
    /// </summary>
    [JsonPropertyName("supportedFeatures")]
    public required List<string> SupportedFeatures { get; init; }

    /// <summary>
    /// Capability limits and bounds.
    /// </summary>
    [JsonPropertyName("limits")]
    public required Dictionary<string, object> Limits { get; init; }

    /// <summary>
    /// Forbidden behaviors for this capability.
    /// </summary>
    [JsonPropertyName("forbidden")]
    public List<ForbiddenBehavior>? Forbidden { get; init; }

    /// <summary>
    /// Template paths for code generation.
    /// </summary>
    [JsonPropertyName("templates")]
    public required CapabilityTemplates Templates { get; init; }
}

public record ForbiddenBehavior
{
    [JsonPropertyName("behavior")]
    public required string Behavior { get; init; }

    [JsonPropertyName("reason")]
    public required string Reason { get; init; }
}

public record CapabilityTemplates
{
    [JsonPropertyName("typescript")]
    public required string Typescript { get; init; }

    [JsonPropertyName("css")]
    public required string Css { get; init; }
}
