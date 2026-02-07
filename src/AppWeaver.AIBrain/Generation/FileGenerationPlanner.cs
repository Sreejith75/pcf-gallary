using System.Text.Json;
using AppWeaver.AIBrain.Configuration;
using AppWeaver.AIBrain.Logging;
using AppWeaver.AIBrain.Models.Specs;
using Microsoft.Extensions.Options;

namespace AppWeaver.AIBrain.Generation;

/// <summary>
/// C# Authority for file generation planning.
/// Enforces deterministic mapping of ComponentSpec to files.
/// NO AI allowed.
/// </summary>
public class FileGenerationPlanner : IFileGenerationPlanner
{
    private readonly BrainOptions _options;

    public FileGenerationPlanner(IOptions<BrainOptions> options)
    {
        _options = options.Value;
    }

    /// <inheritdoc />
    public FileGenerationPlan CreatePlan(
        ComponentSpec spec,
        CancellationToken cancellationToken = default)
    {
        // 1. Validate inputs
        if (spec == null) throw new ArgumentNullException(nameof(spec));

        // 2. Define Deterministic File Mapping Rules (Hardcoded for v1.0)
        // Order | Template | Output
        var steps = new List<FileGenerationStep>
        {
            new() { Order = 1, TemplateName = "ControlManifest.Input.xml.hbs", OutputPath = "ControlManifest.Input.xml", Required = true },
            new() { Order = 2, TemplateName = "package.json.hbs", OutputPath = "package.json", Required = true },
            new() { Order = 3, TemplateName = "tsconfig.json.hbs", OutputPath = "tsconfig.json", Required = true },
            new() { Order = 4, TemplateName = "index.ts.hbs", OutputPath = "index.ts", Required = true },
            new() { Order = 5, TemplateName = "css/component.css.hbs", OutputPath = $"css/{spec.ComponentName}.css", Required = true },
            new() { Order = 6, TemplateName = "strings/strings.resx.hbs", OutputPath = $"strings/{spec.ComponentName}.resx", Required = true },
            new() { Order = 7, TemplateName = "README.md.hbs", OutputPath = "README.md", Required = true },
            new() { Order = 8, TemplateName = ".gitignore.hbs", OutputPath = ".gitignore", Required = true }
        };

        // 3. Create Plan
        var plan = new FileGenerationPlan
        {
            Version = BrainContracts.Version,
            ComponentType = spec.ComponentType,
            Steps = steps
        };

        // 4. Validate Plan (Strict)
        ValidatePlan(plan);

        // 5. Log
        BrainLogger.LogOperation(
            $"plan_{DateTime.UtcNow:yyyyMMddHHmmss}",
            "PlanFileGeneration",
            "Planned",
            0, // Instant
            metadata: new
            {
                componentType = spec.ComponentType,
                files = plan.Steps.Count,
                version = plan.Version
            });

        return plan;
    }

    private void ValidatePlan(FileGenerationPlan plan)
    {
        // Rule 1: Version must match contract
        if (plan.Version != BrainContracts.Version)
        {
            throw new InvalidOperationException($"Plan version mismatch: Expected {BrainContracts.Version}, got {plan.Version}");
        }

        // Rule 2: Must have exactly 8 steps
        if (plan.Steps.Count != 8)
        {
            throw new InvalidOperationException($"Plan must have exactly 8 steps, got {plan.Steps.Count}");
        }

        // Rule 3: Order must be sequential (1..8)
        for (int i = 0; i < plan.Steps.Count; i++)
        {
            if (plan.Steps[i].Order != i + 1)
            {
                throw new InvalidOperationException($"Plan steps must be sequential. Expected order {i + 1}, got {plan.Steps[i].Order}");
            }
        }
    }
}
