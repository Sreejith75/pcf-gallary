using AppWeaver.AIBrain.Abstractions;
using AppWeaver.AIBrain.Models.Capabilities;
using AppWeaver.AIBrain.Models.Specs;

namespace AppWeaver.AIBrain.Validation;

/// <summary>
/// Validates component specifications against capability bounds and rules.
/// </summary>
public class CapabilityValidator
{
    /// <summary>
    /// Validates that a ComponentSpec adheres to its capability's bounds and forbidden behaviors.
    /// </summary>
    public SpecValidationResult ValidateAgainstCapability(
        ComponentSpec spec,
        ComponentCapability capability)
    {
        var errors = new List<ValidationError>();
        var warnings = new List<ValidationWarning>();

        // Validate features
        foreach (var feature in spec.Capabilities.Features)
        {
            if (!capability.SupportedFeatures.Contains(feature))
            {
                errors.Add(new ValidationError
                {
                    RuleId = "CAP_FEATURE_001",
                    Message = $"Feature '{feature}' is not supported by capability '{capability.CapabilityId}'",
                    Suggestion = $"Supported features: {string.Join(", ", capability.SupportedFeatures)}",
                    AutoFixable = false
                });
            }
        }

        // Validate customizations against limits
        if (spec.Capabilities.Customizations != null)
        {
            foreach (var (key, value) in spec.Capabilities.Customizations)
            {
                if (capability.Limits.TryGetValue($"max{char.ToUpper(key[0])}{key.Substring(1)}", out var limitObj))
                {
                    if (limitObj is int maxLimit && value is int actualValue && actualValue > maxLimit)
                    {
                        errors.Add(new ValidationError
                        {
                            RuleId = "CAP_LIMIT_001",
                            Message = $"Customization '{key}' value {actualValue} exceeds limit of {maxLimit}",
                            Suggestion = $"Set '{key}' to a value <= {maxLimit}",
                            AutoFixable = false
                        });
                    }
                }
            }
        }

        // Check forbidden behaviors
        if (capability.Forbidden != null)
        {
            foreach (var forbidden in capability.Forbidden)
            {
                // This would require analyzing the spec for forbidden patterns
                // For now, we'll add a placeholder
                warnings.Add(new ValidationWarning
                {
                    RuleId = "CAP_FORBIDDEN_001",
                    Message = $"Ensure component does not use forbidden behavior: {forbidden.Behavior}",
                    Suggestion = forbidden.Reason
                });
            }
        }

        return new SpecValidationResult
        {
            Version = BrainContracts.Version,
            IsValid = errors.Count == 0,
            Errors = errors,
            Warnings = warnings,
            Downgrades = Array.Empty<ValidationDowngrade>(),
            TotalRules = 3,
            PassedRules = 3 - errors.Count
        };
    }
}
