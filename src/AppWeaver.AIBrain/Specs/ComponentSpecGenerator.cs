using System.Diagnostics;
using System.Text.Json;
using AppWeaver.AIBrain.Configuration;
using AppWeaver.AIBrain.Logging;
using AppWeaver.AIBrain.Models.Capabilities;
using AppWeaver.AIBrain.Models.Intent;
using AppWeaver.AIBrain.Models.Specs;
using AppWeaver.AIBrain.Validation;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;

namespace AppWeaver.AIBrain.Specs;

/// <summary>
/// C# authority layer for governing AI-generated ComponentSpec objects.
/// Invokes Node.js spec generator, validates output, and enforces strict rules.
/// </summary>
public class ComponentSpecGenerator : IComponentSpecGenerator
{
    private readonly BrainOptions _options;
    private readonly string _nodeExecutorPath;
    private readonly RuleValidator _ruleValidator;

    public ComponentSpecGenerator(
        IOptions<BrainOptions> options,
        RuleValidator ruleValidator)
    {
        _options = options.Value;
        _ruleValidator = ruleValidator;
        _nodeExecutorPath = Path.GetFullPath(
            Path.Combine(_options.BrainRootPath, "../executor"));
    }

    /// <inheritdoc />
    public async Task<ComponentSpec> GenerateAsync(
        GlobalIntent intent,
        ComponentCapability capability,
        CancellationToken cancellationToken = default)
    {
        var buildId = $"spec_{DateTime.UtcNow:yyyyMMddHHmmss}";
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // STEP 1: Invoke Node.js Spec Generator (AI)
            var inputPath = await WriteInputJsonAsync(intent, capability, cancellationToken);
            var resultFilePath = await CallNodeJsGeneratorAsync(inputPath, cancellationToken);

            // STEP 2: Read AI Output
            var jsonContent = await File.ReadAllTextAsync(resultFilePath, cancellationToken);

            // STEP 3: Validate JSON Structural Integrity & Contract
            var spec = DeserializeAndValidateStructure(jsonContent);

            // STEP 4: Validate Contract Version
            ValidateContractVersion(spec);

            // STEP 5: Validate Schema Compliance (Strict)
            // Note: JsonSerializer deserialization above implicitly handles basic type validation,
            // but strict schema compliance prevents extra properties. 
            // In this implementation, we rely on the strictly typed C# model AND check for known violations.
            ValidateSchemaCompliance(spec);

            // STEP 6: Capability Enforcement
            ValidateCapability(spec, capability);

            // STEP 7: Rule Validation Hook
            ValidateRules(spec);

            stopwatch.Stop();

            BrainLogger.LogOperation(
                buildId,
                "GenerateComponentSpec",
                "Accepted",
                stopwatch.ElapsedMilliseconds,
                metadata: new
                {
                    componentType = spec.ComponentType,
                    version = spec.Version,
                    propertiesCount = spec.Properties?.Count ?? 0
                });

            return spec;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            string failureReason = ex.GetType().Name;
            if (ex is ComponentSpecContractViolationException) failureReason = "ContractViolation";
            else if (ex is CapabilityViolationException) failureReason = "CapabilityViolation";
            else if (ex is ComponentSpecSchemaValidationException) failureReason = "SchemaViolation";

            BrainLogger.LogOperation(
                buildId,
                "GenerateComponentSpec",
                "Rejected",
                stopwatch.ElapsedMilliseconds,
                errorMessage: ex.Message,
                metadata: new
                {
                    reason = failureReason
                });

            throw;
        }
    }

    private async Task<string> WriteInputJsonAsync(GlobalIntent intent, ComponentCapability capability, CancellationToken cancellationToken)
    {
        var input = new
        {
            globalIntent = intent,
            capability = capability
        };

        var inputPath = $"/tmp/spec-input-{Guid.NewGuid()}.json";
        var json = JsonSerializer.Serialize(input, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(inputPath, json, cancellationToken);
        return inputPath;
    }

    private async Task<string> CallNodeJsGeneratorAsync(string inputPath, CancellationToken cancellationToken)
    {
        var executorUrl = Environment.GetEnvironmentVariable("EXECUTOR_URL");

        if (!string.IsNullOrEmpty(executorUrl))
        {
            try
            {
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromMinutes(2);

                var jsonContent = await File.ReadAllTextAsync(inputPath, cancellationToken);
                var inputObj = JsonSerializer.Deserialize<JsonElement>(jsonContent);

                var payload = new
                {
                    inputJson = inputObj,
                    brainPath = _options.BrainRootPath
                };

                var response = await client.PostAsJsonAsync(
                    $"{executorUrl.TrimEnd('/')}/spec", 
                    payload, 
                    cancellationToken);

                response.EnsureSuccessStatusCode();

                var jsonResult = await response.Content.ReadAsStringAsync(cancellationToken);
                var outputPath = "/tmp/spec-result.json";
                await File.WriteAllTextAsync(outputPath, jsonResult, cancellationToken);

                return outputPath;
            }
            catch (Exception ex)
            {
                throw new ComponentSpecGenerationException(
                    $"Remote executor failed: {ex.Message}", ex);
            }
        }

        var scriptPath = Path.Combine(_nodeExecutorPath, "spec-generator.js");
        if (!File.Exists(scriptPath))
        {
            throw new ComponentSpecGenerationException($"Spec generator script not found: {scriptPath}");
        }

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "node",
                Arguments = $"\"{scriptPath}\" \"{inputPath}\" \"{_options.BrainRootPath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        var errorBuilder = new System.Text.StringBuilder();
        process.ErrorDataReceived += (s, e) => { if (e.Data != null) errorBuilder.AppendLine(e.Data); };

        process.Start();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync(cancellationToken);

        if (process.ExitCode != 0)
        {
            throw new ComponentSpecGenerationException(
                $"Node.js generator failed with exit code {process.ExitCode}. STDERR: {errorBuilder}");
        }

        return "/tmp/spec-result.json";
    }

    private ComponentSpec DeserializeAndValidateStructure(string jsonContent)
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                // Strict handling could be added here if checking for unknown properties during deserialization
            };
            
            var spec = JsonSerializer.Deserialize<ComponentSpec>(jsonContent, options);

            if (spec == null)
            {
                throw new ComponentSpecContractViolationException("Deserialized ComponentSpec is null.");
            }

            // Check required top-level fields (that might be null if JSON was partial)
            if (string.IsNullOrWhiteSpace(spec.ComponentType)) throw new ComponentSpecContractViolationException("Missing componentType.");
            if (string.IsNullOrWhiteSpace(spec.DisplayName)) throw new ComponentSpecContractViolationException("Missing displayName.");
            if (spec.Properties == null) throw new ComponentSpecContractViolationException("Missing properties collection.");

            return spec;
        }
        catch (JsonException ex)
        {
            throw new ComponentSpecContractViolationException($"Invalid JSON structure: {ex.Message}");
        }
    }

    private void ValidateContractVersion(ComponentSpec spec)
    {
        if (string.IsNullOrWhiteSpace(spec.Version))
        {
            throw new ComponentSpecContractViolationException("Missing version field.");
        }

        if (spec.Version != BrainContracts.Version)
        {
            throw new ComponentSpecContractViolationException(
                $"Contract version mismatch. Expected '{BrainContracts.Version}', got '{spec.Version}'.");
        }
    }

    private void ValidateSchemaCompliance(ComponentSpec spec)
    {
        // In a full implementation, we might validate against a JSON Schema file again here
        // to catch extra properties that deserialization ignored.
        // For now, we enforce structural constraints.
        
        // Example: Ensure no nulls in collections
        if (spec.Properties != null && spec.Properties.Any(p => p == null))
        {
             throw new ComponentSpecSchemaValidationException("Properties collection contains null items.");
        }
    }

    private void ValidateCapability(ComponentSpec spec, ComponentCapability capability)
    {
        // 1. Component Type Match
        if (!string.Equals(spec.ComponentType, capability.CapabilityId, StringComparison.OrdinalIgnoreCase))
        {
            throw new CapabilityViolationException(
                $"ComponentType '{spec.ComponentType}' does not match CapabilityId '{capability.CapabilityId}'.");
        }

        // 2. Forbidden Features (if spec exposes features list, which ComponentSpec model currently doesn't deeply show 
        // in previous context, but we check what we can). 
        // Assuming we check properties against capability constraints (not fully detailed in this snippet due to model limits).
        
        // Example: Check if properties count exceeds limit if defined in capability
        // (This would be part of a deeper validation logic, currently we stick to basic capability ID match 
        // as per the requirement "componentType matches ComponentCapability.Id")
    }

    private void ValidateRules(ComponentSpec spec)
    {
        var validationResult = _ruleValidator.ValidateRules(spec);
        if (!validationResult.IsValid)
        {
            var errors = string.Join("; ", validationResult.Errors.Select(e => e.Message));
            throw new CapabilityViolationException($"Rule validation failed: {errors}");
        }
    }
}
