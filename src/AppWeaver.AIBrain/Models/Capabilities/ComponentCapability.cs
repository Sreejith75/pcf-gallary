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
    /// Supported features.
    /// </summary>
    [JsonPropertyName("supportedFeatures")]
    public required List<CapabilityFeature> SupportedFeatures { get; init; }

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
    public CapabilityTemplates? Templates { get; init; }

    /// <summary>
    /// Dependencies.
    /// </summary>
    [JsonPropertyName("dependencies")]
    public CapabilityDependencies? Dependencies { get; init; }
}

public record CapabilityFeature
{
    [JsonPropertyName("featureId")]
    public required string FeatureId { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("description")]
    public required string Description { get; init; }

    [JsonPropertyName("required")]
    public required bool Required { get; init; }

    [JsonPropertyName("configurable")]
    public required bool Configurable { get; init; }

    [JsonPropertyName("parameters")]
    public Dictionary<string, FeatureParameter>? Parameters { get; init; }
}

public record FeatureParameter
{
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonPropertyName("default")]
    public object? Default { get; init; }

    [JsonPropertyName("min")]
    public int? Min { get; init; }

    [JsonPropertyName("max")]
    public int? Max { get; init; }

    [JsonPropertyName("enum")]
    public List<string>? Enum { get; init; }
}

public record ForbiddenBehavior
{
    [JsonPropertyName("behavior")]
    public required string Behavior { get; init; }

    [JsonPropertyName("reason")]
    public required string Reason { get; init; }

    [JsonPropertyName("alternative")]
    public string? Alternative { get; init; }
}

public record CapabilityTemplates
{
    [JsonPropertyName("typescript")]
    public string? Typescript { get; init; }

    [JsonPropertyName("css")]
    public string? Css { get; init; }
}

public record CapabilityDependencies
{
    [JsonPropertyName("pcfVersion")]
    public string? PcfVersion { get; init; }

    [JsonPropertyName("externalLibraries")]
    public List<string>? ExternalLibraries { get; init; }
}
