using AppWeaver.AIBrain.Abstractions;
using AppWeaver.AIBrain.Configuration;
using AppWeaver.AIBrain.Models;
using AppWeaver.AIBrain.Models.Capabilities;
using AppWeaver.AIBrain.Models.Intent;
using AppWeaver.AIBrain.Models.Specs;
using AppWeaver.AIBrain.Validation;
using Microsoft.Extensions.Options;

namespace AppWeaver.AIBrain.Procedures;

/// <summary>
/// Executes the create-component procedure.
/// This orchestrates the full pipeline but does NOT call LLMs directly.
/// Produces an execution plan for the Node.js layer to execute.
/// </summary>
public class CreateComponentProcedure : IProcedureExecutor
{
    private readonly IBrainRouter _router;
    private readonly CapabilityValidator _capabilityValidator;
    private readonly RuleValidator _ruleValidator;
    private readonly BrainOptions _options;

    public CreateComponentProcedure(
        IBrainRouter router,
        CapabilityValidator capabilityValidator,
        RuleValidator ruleValidator,
        IOptions<BrainOptions> options)
    {
        _router = router;
        _capabilityValidator = capabilityValidator;
        _ruleValidator = ruleValidator;
        _options = options.Value;
    }

    /// <inheritdoc />
    public async Task<ComponentExecutionPlan> ExecuteCreateComponentAsync(
        string userPrompt,
        CancellationToken cancellationToken = default)
    {
        var buildId = $"build_{DateTimeOffset.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}".Substring(0, 40);

        // NOTE: This is a simplified version that assumes the LLM has already been called
        // and we have a validated GlobalIntent and ComponentSpec.
        // In the real implementation, this would coordinate with the Node.js layer.

        // For now, we'll create a placeholder execution plan
        // The actual LLM calls happen in the Node.js layer

        throw new NotImplementedException(
            "This method coordinates with the Node.js layer for LLM execution. " +
            "Use ValidateComponentSpecAsync for validation-only operations.");
    }

    /// <inheritdoc />
    public async Task<SpecValidationResult> ValidateComponentSpecAsync(
        ComponentSpec spec,
        CancellationToken cancellationToken = default)
    {
        // Load capability for validation
        var context = await _router.RouteAsync(
            BrainTask.LoadCapability,
            new Dictionary<string, string> { ["capabilityId"] = spec.Capabilities.CapabilityId },
            cancellationToken);

        var capability = context.GetArtifact<ComponentCapability>("capability");
        if (capability == null)
        {
            throw new InvalidOperationException($"Capability '{spec.Capabilities.CapabilityId}' not found");
        }

        // Validate against capability
        var capabilityResult = _capabilityValidator.ValidateAgainstCapability(spec, capability);

        // Validate against rules
        var rulesResult = _ruleValidator.ValidateRules(spec);

        // Merge results
        var allErrors = capabilityResult.Errors.Concat(rulesResult.Errors).ToList();
        var allWarnings = capabilityResult.Warnings.Concat(rulesResult.Warnings).ToList();
        var allDowngrades = capabilityResult.Downgrades.Concat(rulesResult.Downgrades).ToList();

        return new SpecValidationResult
        {
            Version = BrainContracts.Version,
            IsValid = allErrors.Count == 0,
            Errors = allErrors,
            Warnings = allWarnings,
            Downgrades = allDowngrades,
            TotalRules = capabilityResult.TotalRules + rulesResult.TotalRules,
            PassedRules = capabilityResult.PassedRules + rulesResult.PassedRules
        };
    }

    /// <summary>
    /// Creates an execution plan from validated components.
    /// This is called AFTER LLM execution in the Node.js layer.
    /// </summary>
    public ComponentExecutionPlan CreateExecutionPlan(
        GlobalIntent intent,
        string capabilityId,
        ComponentSpec spec,
        SpecValidationResult validationReport)
    {
        // Generate deterministic build ID
        var buildId = IdempotencyHelper.GenerateDeterministicBuildId(intent, capabilityId);

        // Define file generation steps
        var filesToGenerate = new List<FileGenerationStep>
        {
            new()
            {
                Step = 1,
                FileName = "ControlManifest.Input.xml",
                OutputPath = "ControlManifest.Input.xml",
                TemplatePath = "templates/ControlManifest.Input.xml.hbs",
                ValidationType = "xml-schema",
                Required = true
            },
            new()
            {
                Step = 2,
                FileName = "package.json",
                OutputPath = "package.json",
                TemplatePath = "templates/package.json.hbs",
                ValidationType = "json-schema",
                Required = true
            },
            new()
            {
                Step = 3,
                FileName = "tsconfig.json",
                OutputPath = "tsconfig.json",
                TemplatePath = "templates/tsconfig.json.hbs",
                ValidationType = "json-schema",
                Required = true
            },
            new()
            {
                Step = 4,
                FileName = "index.ts",
                OutputPath = "index.ts",
                TemplatePath = $"templates/{capabilityId}/index.ts.hbs",
                ValidationType = "typescript",
                Required = true
            },
            new()
            {
                Step = 5,
                FileName = $"{spec.ComponentName}.css",
                OutputPath = $"css/{spec.ComponentName}.css",
                TemplatePath = $"templates/{capabilityId}/styles.css.hbs",
                ValidationType = "css",
                Required = true
            },
            new()
            {
                Step = 6,
                FileName = $"{spec.ComponentName}.resx",
                OutputPath = $"strings/{spec.ComponentName}.resx",
                TemplatePath = "templates/strings.resx.hbs",
                ValidationType = "resx",
                Required = true
            },
            new()
            {
                Step = 7,
                FileName = "README.md",
                OutputPath = "README.md",
                TemplatePath = "templates/README.md.hbs",
                ValidationType = "markdown",
                Required = false
            },
            new()
            {
                Step = 8,
                FileName = ".gitignore",
                OutputPath = ".gitignore",
                TemplatePath = "templates/.gitignore.hbs",
                ValidationType = "none",
                Required = false
            }
        };

        return new ComponentExecutionPlan
        {
            Version = BrainContracts.Version,
            BuildId = buildId,
            Intent = intent,
            CapabilityId = capabilityId,
            ComponentSpec = spec,
            FilesToGenerate = filesToGenerate,
            ValidationReport = validationReport
        };
    }
}
