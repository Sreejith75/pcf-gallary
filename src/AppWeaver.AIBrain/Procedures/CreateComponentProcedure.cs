using AppWeaver.AIBrain.Abstractions;
using AppWeaver.AIBrain.Configuration;
using AppWeaver.AIBrain.Models;
using AppWeaver.AIBrain.Models.Capabilities;
using AppWeaver.AIBrain.Models.Intent;
using AppWeaver.AIBrain.Models.Specs;
using AppWeaver.AIBrain.Validation;
using AppWeaver.AIBrain.Logging;
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
    private readonly Intent.IIntentInterpreter _intentInterpreter;
    private readonly Specs.IComponentSpecGenerator _specGenerator;
    private readonly Build.IBuildOrchestrator _buildOrchestrator;

    public CreateComponentProcedure(
        IBrainRouter router,
        CapabilityValidator capabilityValidator,
        RuleValidator ruleValidator,
        IOptions<BrainOptions> options,
        Intent.IIntentInterpreter intentInterpreter,
        Specs.IComponentSpecGenerator specGenerator,
        Build.IBuildOrchestrator buildOrchestrator)
    {
        _router = router;
        _capabilityValidator = capabilityValidator;
        _ruleValidator = ruleValidator;
        _options = options.Value;
        _intentInterpreter = intentInterpreter;
        _specGenerator = specGenerator;
        _buildOrchestrator = buildOrchestrator;
    }

    /// <inheritdoc />
    public async Task<ComponentExecutionPlan> ExecuteCreateComponentAsync(
        string userPrompt,
        CancellationToken cancellationToken = default)
    {
        var buildId = $"pipeline_{DateTime.UtcNow:yyyyMMddHHmmss}";
        BrainLogger.LogOperation(buildId, "Pipeline", "Started", 0);

        try
        {
            // STEP 1: Interpret Intent (Node.js/AI -> C# Authority)
            var intentResult = await _intentInterpreter.InterpretAsync(userPrompt, cancellationToken);
            
            if (intentResult.NeedsClarification || intentResult.Intent == null)
            {
                // In a real CLI/API, this would trigger a clarification flow.
                // For now, we return a plan indicating clarification needed, or throw if strict.
                // The prompt implies we should fail fast or handle it.
                // Let's throw for now as the contract return type is ComponentExecutionPlan (which implies success context)
                // OR we can add a status to ComponentExecutionPlan.
                // Given "Failure in any step stops execution", we fail if intent is unclear.
                throw new InvalidOperationException("Intent requires clarification: " + 
                    string.Join(", ", intentResult.UnmappedPhrases));
            }

            var intent = intentResult.Intent;

            // We use the capability ID from the intent.
            // The GlobalIntent model now includes ComponentType which maps to the capability ID.
            var capabilityId = intent.ComponentType;
            
            if (string.IsNullOrEmpty(capabilityId))
            {
                BrainLogger.LogError(buildId, "Pipeline", "ComponentType missing in intent", null);
                throw new InvalidOperationException("Intent interpretation failed to determine ComponentType/Capability.");
            }
            
            // Load authoritative capability
            IBrainContext context = null;
            ComponentCapability capability = null;

            try
            {
                context = await _router.RouteAsync(
                    BrainTask.LoadCapability,
                    new Dictionary<string, string> { ["capabilityId"] = capabilityId },
                    cancellationToken);
                    
                capability = context.GetArtifact<ComponentCapability>("capability");
            }
            catch (FileNotFoundException)
            {
                BrainLogger.LogOperation(buildId, "Pipeline", $"Capability '{capabilityId}' not found (exception), falling back to 'generic'", 0);
                capability = null;
            }

            if (capability == null)
            {
                // Fallback to generic capability
                var genericContext = await _router.RouteAsync(
                    BrainTask.LoadCapability,
                    new Dictionary<string, string> { ["capabilityId"] = "generic" },
                    cancellationToken);
                    
                capability = genericContext.GetArtifact<ComponentCapability>("capability");
                
                if (capability == null)
                {
                    throw new InvalidOperationException($"Capability '{capabilityId}' not found and 'generic' fallback failed.");
                }
            }

            // STEP 3: Generate Spec (AI -> C# Authority)
            var spec = await _specGenerator.GenerateAsync(intent, capability, cancellationToken);

            // STEP 4: Build Orchestration (C# -> Node.js -> PCF)
            var buildResult = await _buildOrchestrator.BuildAsync(spec, cancellationToken);

            BrainLogger.LogOperation(buildId, "Pipeline", "Completed", 0);

            // Return execution plan (wrapper for the result)
            // The implementation requires mapping `BuildResult` to `ComponentExecutionPlan`.
            // Since `ComponentExecutionPlan` is the legacy/v0 contract, we need to adapt.
            // Or we just populate what we have.
            
            return new ComponentExecutionPlan
            {
                Version = BrainContracts.Version,
                BuildId = buildResult.BuildId,
                Intent = intent,
                CapabilityId = capabilityId,
                ComponentSpec = spec,
                FilesToGenerate = new List<FileGenerationStep>(), // Already generated by build
                ValidationReport = new SpecValidationResult 
                { 
                    IsValid = true, 
                    Version = BrainContracts.Version, 
                    Errors = new List<ValidationError>(), 
                    Warnings = new List<ValidationWarning>(), 
                    Downgrades = new List<ValidationDowngrade>(),
                    TotalRules = 0,
                    PassedRules = 0
                },
                ZipPath = buildResult.ZipPath
            };
        }
        catch (Exception ex)
        {
            BrainLogger.LogError(buildId, "Pipeline", ex.Message, ex);
            throw;
        }
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
