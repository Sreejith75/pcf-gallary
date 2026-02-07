using System.Diagnostics;
using System.Text.Json;
using AppWeaver.AIBrain.Configuration;
using AppWeaver.AIBrain.Generation;
using AppWeaver.AIBrain.Logging;
using AppWeaver.AIBrain.Models.Specs;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;

namespace AppWeaver.AIBrain.Build;

/// <summary>
/// C# Orchestrator for the PCF build pipeline.
/// Coordinates file generation and PCF CLI execution.
/// NO AI allowed in this phase.
/// </summary>
public class BuildOrchestrator : IBuildOrchestrator
{
    private readonly BrainOptions _options;
    private readonly IFileGenerationPlanner _planner;
    private readonly string _nodeExecutorPath;

    public BuildOrchestrator(
        IOptions<BrainOptions> options,
        IFileGenerationPlanner planner)
    {
        _options = options.Value;
        _planner = planner;
        _nodeExecutorPath = Path.GetFullPath(
            Path.Combine(_options.BrainRootPath, "../executor"));
    }

    /// <inheritdoc />
    public async Task<BuildResult> BuildAsync(
        ComponentSpec spec,
        CancellationToken cancellationToken = default)
    {
        var buildId = $"build_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid().ToString("N")[..6]}";
        var stopwatch = Stopwatch.StartNew();
        var workingDir = Path.Combine("/tmp/pcf-build", buildId);

        BrainLogger.LogOperation(buildId, "BuildOrchestration", "Started", 0);

        try
        {
            // STEP 1: Create Working Directory
            Directory.CreateDirectory(workingDir);

            // STEP 2: Plan File Generation (C# Authority)
            var plan = _planner.CreatePlan(spec, cancellationToken);
            
            // Serialize Plan & Spec for Node.js
            var buildInput = new
            {
                version = BrainContracts.Version,
                componentSpec = spec,
                fileGenerationPlan = plan
            };
            
            var inputPath = Path.Combine(workingDir, "build-input.json");
            var jsonOptions = new JsonSerializerOptions 
            { 
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            await File.WriteAllTextAsync(inputPath, JsonSerializer.Serialize(buildInput, jsonOptions), cancellationToken);

            // STEP 3: Invoke File Generator (Node.js)
            await ExecuteNodeScriptAsync("file-generator.js", $"\"{inputPath}\" \"{workingDir}\"", cancellationToken);
            
            // Verify files exist
            foreach (var step in plan.Steps)
            {
                var filePath = Path.Combine(workingDir, step.OutputPath);
                if (!File.Exists(filePath))
                {
                    throw new BuildOrchestrationException($"Required file missing after generation: {step.OutputPath}");
                }
            }

            // STEP 4: Invoke Build Executor (Node.js - PCF CLI)
            // Passes working directory where files are located
            await ExecuteNodeScriptAsync("build-executor.js", $"\"{workingDir}\" \"{spec.ComponentName}\"", cancellationToken);

            // STEP 5: Verify ZIP Output
            // The executor is expected to create: {ComponentName}_{buildId}.zip OR just a zip in the folder
            // Let's assume standard PCF packing or strict naming.
            // Requirement says: "bin/Release/*.zip" or specifically named.
            // Part 2 spec says: "zip -r {ComponentName}_{buildId}.zip ."
            var expectedZipName = $"{spec.ComponentName}_{buildId}.zip";
            var outputZipPath = Path.Combine(workingDir, expectedZipName);

            if (!File.Exists(outputZipPath))
            {
                // Fallback check if simple name was used or located elsewhere?
                // Strict requirement: "Verify ZIP file must exist"
                throw new BuildOrchestrationException($"Outcome ZIP file not found: {outputZipPath}");
            }

            stopwatch.Stop();
            BrainLogger.LogOperation(
                buildId, 
                "BuildOrchestration", 
                "Completed", 
                stopwatch.ElapsedMilliseconds,
                metadata: new { zip = outputZipPath });

            return new BuildResult
            {
                BuildId = buildId,
                ZipPath = outputZipPath,
                Success = true
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            BrainLogger.LogError(buildId, "BuildOrchestration", ex.Message, ex);
            throw new BuildOrchestrationException("Build failed", ex);
        }
    }

    private async Task ExecuteNodeScriptAsync(string scriptName, string arguments, CancellationToken cancellationToken)
    {
        var executorUrl = Environment.GetEnvironmentVariable("EXECUTOR_URL");
        if (!string.IsNullOrEmpty(executorUrl))
        {
            try
            {
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromMinutes(10); // Build operations can be slow

                if (scriptName == "file-generator.js")
                {
                    // Args: "inputPath" "workingDir"
                    var args = ParseArgs(arguments);
                    var inputPath = args[0];
                    var workingDir = args[1];
                    
                    var jsonContent = await File.ReadAllTextAsync(inputPath, cancellationToken);
                    var inputObj = JsonSerializer.Deserialize<JsonElement>(jsonContent);
                    
                    var payload = new { inputJson = inputObj, outputDir = workingDir };
                    var response = await client.PostAsJsonAsync($"{executorUrl.TrimEnd('/')}/files", payload, cancellationToken);
                    response.EnsureSuccessStatusCode();
                    return;
                }
                else if (scriptName == "build-executor.js")
                {
                    // Args: "workingDir" "componentName"
                    var args = ParseArgs(arguments);
                    var workingDir = args[0];
                    var componentName = args[1];

                    var payload = new { workingDir, componentName };
                    var response = await client.PostAsJsonAsync($"{executorUrl.TrimEnd('/')}/build", payload, cancellationToken);
                    response.EnsureSuccessStatusCode();
                    return;
                }
            }
            catch (Exception ex)
            {
                throw new BuildOrchestrationException(
                    $"Remote executor failed for {scriptName}: {ex.Message}", ex);
            }
        }

        var scriptPath = Path.Combine(_nodeExecutorPath, scriptName);
        if (!File.Exists(scriptPath))
        {
            throw new BuildOrchestrationException($"Executor script not found: {scriptPath}");
        }

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "node",
                Arguments = $"\"{scriptPath}\" {arguments}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        var outputBuilder = new System.Text.StringBuilder();
        var errorBuilder = new System.Text.StringBuilder();

        process.OutputDataReceived += (s, e) => { if (e.Data != null) outputBuilder.AppendLine(e.Data); };
        process.ErrorDataReceived += (s, e) => { if (e.Data != null) errorBuilder.AppendLine(e.Data); };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync(cancellationToken);

        if (process.ExitCode != 0)
        {
            throw new BuildOrchestrationException(
                $"Node.js script {scriptName} failed (Exit: {process.ExitCode}).\nSTDERR: {errorBuilder}\nSTDOUT: {outputBuilder}");
        }
    }

    private string[] ParseArgs(string arguments)
    {
        // Simple split strictly for this usage
        // arguments are quoted strings separated by space e.g. "path1" "path2"
        return arguments.Split(new[] { "\" \"" }, StringSplitOptions.None)
                        .Select(s => s.Trim('\"', ' '))
                        .ToArray();
    }
}
