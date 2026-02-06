using System.Text.Json.Serialization;

namespace AppWeaver.AIBrain.Models.Specs;

/// <summary>
/// Represents a complete PCF component specification.
/// Maps to ai-brain/schemas/component-spec.schema.json
/// </summary>
public record ComponentSpec
{
    /// <summary>
    /// Component identifier (kebab-case).
    /// </summary>
    [JsonPropertyName("componentId")]
    public required string ComponentId { get; init; }

    /// <summary>
    /// Component name (PascalCase).
    /// </summary>
    [JsonPropertyName("componentName")]
    public required string ComponentName { get; init; }

    /// <summary>
    /// Namespace (PascalCase).
    /// </summary>
    [JsonPropertyName("namespace")]
    public required string Namespace { get; init; }

    /// <summary>
    /// Display name for PowerApps.
    /// </summary>
    [JsonPropertyName("displayName")]
    public required string DisplayName { get; init; }

    /// <summary>
    /// Component description.
    /// </summary>
    [JsonPropertyName("description")]
    public required string Description { get; init; }

    /// <summary>
    /// Capability configuration.
    /// </summary>
    [JsonPropertyName("capabilities")]
    public required CapabilityConfig Capabilities { get; init; }

    /// <summary>
    /// Component properties.
    /// </summary>
    [JsonPropertyName("properties")]
    public required List<ComponentProperty> Properties { get; init; }

    /// <summary>
    /// Resource configuration.
    /// </summary>
    [JsonPropertyName("resources")]
    public required ResourceConfig Resources { get; init; }

    /// <summary>
    /// Validation metadata.
    /// </summary>
    [JsonPropertyName("validation")]
    public ValidationMetadata? Validation { get; init; }
}

public record CapabilityConfig
{
    [JsonPropertyName("capabilityId")]
    public required string CapabilityId { get; init; }

    [JsonPropertyName("features")]
    public required List<string> Features { get; init; }

    [JsonPropertyName("customizations")]
    public Dictionary<string, object>? Customizations { get; init; }
}

public record ComponentProperty
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("displayName")]
    public required string DisplayName { get; init; }

    [JsonPropertyName("dataType")]
    public required string DataType { get; init; }

    [JsonPropertyName("usage")]
    public required string Usage { get; init; }

    [JsonPropertyName("required")]
    public required bool Required { get; init; }

    [JsonPropertyName("description")]
    public required string Description { get; init; }
}

public record ResourceConfig
{
    [JsonPropertyName("code")]
    public required string Code { get; init; }

    [JsonPropertyName("css")]
    public required List<string> Css { get; init; }

    [JsonPropertyName("resx")]
    public required List<string> Resx { get; init; }
}

public record ValidationMetadata
{
    [JsonPropertyName("rulesApplied")]
    public required List<string> RulesApplied { get; init; }

    [JsonPropertyName("warnings")]
    public required List<string> Warnings { get; init; }

    [JsonPropertyName("downgrades")]
    public required List<string> Downgrades { get; init; }
}
