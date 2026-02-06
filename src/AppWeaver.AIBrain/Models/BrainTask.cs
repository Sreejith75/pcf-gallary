namespace AppWeaver.AIBrain.Models;

/// <summary>
/// Defines the type of brain task to be executed.
/// Each task type determines which brain artifacts are loaded and what validation is performed.
/// </summary>
public enum BrainTask
{
    /// <summary>
    /// Parse natural language prompt into structured GlobalIntent.
    /// Loads: global-intent schema, intent-mapping rules, ambiguity-resolution rules
    /// </summary>
    InterpretIntent,

    /// <summary>
    /// Match GlobalIntent to a specific capability.
    /// Loads: capability registry, matched capability definition
    /// </summary>
    MatchCapability,

    /// <summary>
    /// Generate ComponentSpec from GlobalIntent and Capability.
    /// Loads: component-spec schema, capability definition, PCF rules
    /// </summary>
    GenerateComponentSpec,

    /// <summary>
    /// Validate ComponentSpec against all rules.
    /// Loads: PCF core rules, accessibility rules, performance rules
    /// </summary>
    ValidateRules,

    /// <summary>
    /// Perform final cross-reference validation.
    /// Loads: component-spec schema, capability bounds
    /// </summary>
    ValidateFinal,

    /// <summary>
    /// Load schema for ad-hoc validation.
    /// Loads: specified schema file
    /// </summary>
    LoadSchema,

    /// <summary>
    /// Load capability for ad-hoc operations.
    /// Loads: specified capability file
    /// </summary>
    LoadCapability,

    /// <summary>
    /// Load prompt template for LLM execution.
    /// Loads: specified prompt file
    /// </summary>
    LoadPrompt
}
