using AppWeaver.AIBrain.Models.Capabilities;
using AppWeaver.AIBrain.Models.Intent;
using AppWeaver.AIBrain.Models.Specs;

namespace AppWeaver.AIBrain.Specs;

/// <summary>
/// Interface for component specification generation.
/// Acts as the gatekeeper between AI and code generation.
/// </summary>
public interface IComponentSpecGenerator
{
    /// <summary>
    /// Generates a validated ComponentSpec from intent and capability.
    /// </summary>
    /// <param name="intent">The validated global intent.</param>
    /// <param name="capability">The authoritative capability definition.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A trusted, validated ComponentSpec.</returns>
    /// <exception cref="ComponentSpecGenerationException">When generation fails (AI error, execution error).</exception>
    /// <exception cref="ComponentSpecContractViolationException">When JSON structure or contract version is invalid.</exception>
    /// <exception cref="ComponentSpecSchemaValidationException">When usage violates the schema.</exception>
    /// <exception cref="CapabilityViolationException">When the spec violates capability constraints.</exception>
    Task<ComponentSpec> GenerateAsync(
        GlobalIntent intent,
        ComponentCapability capability,
        CancellationToken cancellationToken = default
    );
}

public class ComponentSpecGenerationException : Exception
{
    public ComponentSpecGenerationException(string message) : base(message) { }
    public ComponentSpecGenerationException(string message, Exception inner) : base(message, inner) { }
}

public class ComponentSpecContractViolationException : Exception
{
    public ComponentSpecContractViolationException(string message) : base(message) { }
}

public class ComponentSpecSchemaValidationException : Exception
{
    public ComponentSpecSchemaValidationException(string message) : base(message) { }
}

public class CapabilityViolationException : Exception
{
    public CapabilityViolationException(string message) : base(message) { }
}
