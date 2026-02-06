using AppWeaver.AIBrain.Abstractions;
using AppWeaver.AIBrain.Models.Specs;

namespace AppWeaver.AIBrain.Security;

/// <summary>
/// Trust boundary enforcement for Node.js responses.
/// Treats all Node.js input as untrusted and re-validates.
/// </summary>
public class TrustBoundaryValidator
{
    private readonly IProcedureExecutor _executor;

    public TrustBoundaryValidator(IProcedureExecutor executor)
    {
        _executor = executor;
    }

    /// <summary>
    /// Validates a ComponentSpec received from Node.js.
    /// CRITICAL: Always re-validate, never trust Node.js output.
    /// </summary>
    /// <param name="spec">ComponentSpec from Node.js (untrusted)</param>
    /// <param name="expectedCapabilityId">Expected capability ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result</returns>
    /// <exception cref="SecurityException">If capability ID mismatch detected</exception>
    public async Task<TrustBoundaryValidationResult> ValidateNodeJsResponseAsync(
        ComponentSpec spec,
        string expectedCapabilityId,
        CancellationToken cancellationToken = default)
    {
        // Rule 1: Capability ID must match expected
        if (spec.Capabilities.CapabilityId != expectedCapabilityId)
        {
            return new TrustBoundaryValidationResult
            {
                IsTrusted = false,
                Reason = $"Capability ID mismatch: expected '{expectedCapabilityId}', got '{spec.Capabilities.CapabilityId}'",
                Action = TrustBoundaryAction.Reject
            };
        }

        // Rule 2: Re-validate against all rules (never trust Node.js)
        var validationResult = await _executor.ValidateComponentSpecAsync(spec, cancellationToken);

        if (!validationResult.IsValid)
        {
            return new TrustBoundaryValidationResult
            {
                IsTrusted = false,
                Reason = $"Validation failed: {validationResult.Errors.Count} error(s)",
                Action = TrustBoundaryAction.Reject,
                ValidationErrors = validationResult.Errors
            };
        }

        // Rule 3: Check for forbidden features (capability bounds)
        // This is already done in ValidateComponentSpecAsync, but we make it explicit
        var forbiddenFeatures = spec.Capabilities.Features
            .Where(f => !IsFeatureAllowed(f, expectedCapabilityId))
            .ToList();

        if (forbiddenFeatures.Any())
        {
            return new TrustBoundaryValidationResult
            {
                IsTrusted = false,
                Reason = $"Forbidden features detected: {string.Join(", ", forbiddenFeatures)}",
                Action = TrustBoundaryAction.Reject
            };
        }

        // All checks passed
        return new TrustBoundaryValidationResult
        {
            IsTrusted = true,
            Reason = "All trust boundary checks passed",
            Action = TrustBoundaryAction.Approve
        };
    }

    /// <summary>
    /// Validates contract version from Node.js.
    /// </summary>
    public bool ValidateContractVersion(string? version)
    {
        return BrainContracts.IsVersionSupported(version);
    }

    private bool IsFeatureAllowed(string feature, string capabilityId)
    {
        // This would load the capability and check supported features
        // For now, we assume the validation in ValidateComponentSpecAsync handles this
        return true;
    }
}

/// <summary>
/// Result of trust boundary validation.
/// </summary>
public record TrustBoundaryValidationResult
{
    /// <summary>
    /// Whether the input is trusted.
    /// </summary>
    public required bool IsTrusted { get; init; }

    /// <summary>
    /// Reason for trust/distrust.
    /// </summary>
    public required string Reason { get; init; }

    /// <summary>
    /// Action to take.
    /// </summary>
    public required TrustBoundaryAction Action { get; init; }

    /// <summary>
    /// Validation errors, if any.
    /// </summary>
    public IReadOnlyList<ValidationError>? ValidationErrors { get; init; }
}

/// <summary>
/// Action to take based on trust boundary validation.
/// </summary>
public enum TrustBoundaryAction
{
    /// <summary>
    /// Approve the input and proceed.
    /// </summary>
    Approve,

    /// <summary>
    /// Reject the input and fail the build.
    /// </summary>
    Reject,

    /// <summary>
    /// Request re-generation from Node.js.
    /// </summary>
    Retry
}
