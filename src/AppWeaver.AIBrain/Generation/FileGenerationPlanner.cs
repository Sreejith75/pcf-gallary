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

        // 2. Define Deterministic File Mapping Rules
        // We now use capability-specific templates where appropriate.
        var capabilityId = spec.ComponentType.ToLowerInvariant(); // e.g., "star-rating"
        
        var steps = new List<FileGenerationStep>
        {
            new() { Order = 1, TemplateName = "ControlManifest.Input.xml.hbs", OutputPath = "ControlManifest.Input.xml", Required = true },
            new() { Order = 2, TemplateName = "package.json.hbs", OutputPath = "package.json", Required = true },
            new() { Order = 3, TemplateName = "tsconfig.json.hbs", OutputPath = "tsconfig.json", Required = true },
            
            // Dynamic: Index (Entry point)
            new() { Order = 4, TemplateName = ResolveTemplate(capabilityId, "index.ts.hbs"), OutputPath = "index.ts", Required = true },
            
            // Dynamic: CSS (Styles)
            new() { Order = 5, TemplateName = ResolveTemplate(capabilityId, "styles.css.hbs"), OutputPath = $"css/{spec.ComponentName}.css", Required = true },
            
            new() { Order = 6, TemplateName = "strings/strings.resx.hbs", OutputPath = $"strings/{spec.ComponentName}.resx", Required = true },
            new() { Order = 7, TemplateName = "README.md.hbs", OutputPath = "README.md", Required = true },
            new() { Order = 8, TemplateName = ".gitignore.hbs", OutputPath = ".gitignore", Required = true },
            
            // Dynamic: React View Component
            new() { Order = 9, TemplateName = ResolveTemplate(capabilityId, "Control.tsx.hbs"), OutputPath = $"{spec.ComponentName}View.tsx", Required = true },
            
            // System: Preview Harness
            new() { Order = 10, TemplateName = "preview.tsx.hbs", OutputPath = "preview.tsx", Required = true }
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

    private string ResolveTemplate(string capabilityId, string templateName)
    {
        // Logic: Try templates/{capabilityId}/{templateName}
        // If file exists, return it.
        // Else return templates/generic/{templateName}
        // Note: The planner runs in C# context, but templates are on disk.
        // We need to check existence. `_options.BrainRootPath` gives us the root.
        
        var templatesDir = Path.Combine(_options.BrainRootPath, "templates");
        var specificPath = Path.Combine(templatesDir, capabilityId, templateName);
        
        if (File.Exists(specificPath))
        {
            return $"{capabilityId}/{templateName}";
        }

        // Fallback to generic
        // We assume generic templates exist. If not, the file generator will fail later, which is acceptable.
        BrainLogger.LogOperation("TemplateResolution", "Fallback", $"Using generic template for {templateName} (Capability: {capabilityId})", 0);
        return $"generic/{templateName}";
    }

    private void ValidatePlan(FileGenerationPlan plan)
    {
        // Rule 1: Version must match contract
        if (plan.Version != BrainContracts.Version)
        {
            throw new InvalidOperationException($"Plan version mismatch: Expected {BrainContracts.Version}, got {plan.Version}");
        }

        // Rule 2: Must have exactly 10 steps now (added View and Preview)
        if (plan.Steps.Count != 10)
        {
            throw new InvalidOperationException($"Plan must have exactly 10 steps, got {plan.Steps.Count}");
        }

        // Rule 3: Order must be sequential (1..10)
        for (int i = 0; i < plan.Steps.Count; i++)
        {
            if (plan.Steps[i].Order != i + 1)
            {
                throw new InvalidOperationException($"Plan steps must be sequential. Expected order {i + 1}, got {plan.Steps[i].Order}");
            }
        }
    }
}
