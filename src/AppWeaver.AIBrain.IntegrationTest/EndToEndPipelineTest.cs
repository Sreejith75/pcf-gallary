using AppWeaver.AIBrain.Abstractions;
using AppWeaver.AIBrain.Build;
using AppWeaver.AIBrain.Configuration;
using AppWeaver.AIBrain.Generation;
using AppWeaver.AIBrain.Intent;
using AppWeaver.AIBrain.Logging;
using AppWeaver.AIBrain.Procedures;
using AppWeaver.AIBrain.Specs;
using AppWeaver.AIBrain.Validation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace AppWeaver.AIBrain.IntegrationTest;

/// <summary>
/// Executes the FULL pipeline:
/// Prompt -> Intent (Node.js/AI) -> Spec (Node.js/AI) -> Build (Node.js/PCF) -> ZIP
/// </summary>
public class EndToEndPipelineTest
{
    public static async Task<int> RunAsync()
    {
        Console.WriteLine("=== END-TO-END PIPELINE TEST (REAL AI) ===\n");
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // 1. Setup DI
            var services = new ServiceCollection();
            var brainPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../ai-brain"));
            
            Console.WriteLine($"Brain path: {brainPath}");
            
            // Check if node executors exist
            var executorPath = Path.GetFullPath(Path.Combine(brainPath, "../executor"));
            if (!Directory.Exists(executorPath))
            {
                Console.WriteLine($"❌ Executor directory not found: {executorPath}");
                return 1;
            }

            // Register services
            services.AddAppWeaverAIBrain(brainPath, options =>
            {
                options.DefaultNamespace = "Contoso";
                options.EnableCaching = false;
            });

            // Ensure we have the procedure registered (it's added by AddAppWeaverAIBrain)
            var serviceProvider = services.BuildServiceProvider();
            var procedure = serviceProvider.GetRequiredService<IProcedureExecutor>(); // Should resolve to CreateComponentProcedure

            // 2. Define Input
            var userPrompt = "Create a modern star rating control";
            Console.WriteLine($"\nInput Prompt: \"{userPrompt}\"\n");

            // 3. Execute Pipeline
            Console.WriteLine("STEP 1: Starting Pipeline Execution...");
            
            var result = await procedure.ExecuteCreateComponentAsync(userPrompt);

            // 4. Verify Result
            Console.WriteLine("\n=== PIPELINE COMPLETED ===");
            Console.WriteLine($"Build ID: {result.BuildId}");
            Console.WriteLine($"Intent: {result.Intent.UiIntent.PrimaryPurpose}");
            Console.WriteLine($"Capability: {result.CapabilityId}");
            Console.WriteLine($"Spec Component: {result.ComponentSpec.ComponentName}");
            
            // Files check (files are generated in /tmp/pcf-build/{result.BuildId} by the build orchestrator)
            // The result object might not expose the exact path unless we updated the plan to include it.
            // CreateComponentProcedure returns ComponentExecutionPlan which has BuildId.
            // BuildOrchestrator used /tmp/pcf-build/{buildId}.
            // Note: In CreateComponentProcedure, I used `var buildId = $"pipeline_{...}"`.
            // But BuildOrchestrator generates its OWN buildId inside `BuildAsync`.
            // Wait, my implementation of `CreateComponentProcedure` lines 147 says: `BuildId = buildResult.BuildId`.
            // So result.BuildId IS the directory name.
            
            var buildDir = Path.Combine("/tmp/pcf-build", result.BuildId);
            var zipPath = Path.Combine(buildDir, $"{result.ComponentSpec.ComponentName}_{result.BuildId}.zip");

            Console.WriteLine($"\nVerifying Artifacts in: {buildDir}");
            
            if (Directory.Exists(buildDir))
            {
                Console.WriteLine("✓ Build directory exists");
                
                var files = Directory.GetFiles(buildDir);
                Console.WriteLine($"✓ Found {files.Length} files");

                if (File.Exists(zipPath))
                {
                    Console.WriteLine($"✓ ZIP Artifact created: {zipPath}");
                    Console.WriteLine($"  Size: {new FileInfo(zipPath).Length} bytes");
                }
                else
                {
                    Console.WriteLine($"❌ ZIP Artifact MISSING at expected path: {zipPath}");
                    // List zip files found
                    var zips = Directory.GetFiles(buildDir, "*.zip");
                    if (zips.Any())
                    {
                        Console.WriteLine($"  Found other zips: {string.Join(", ", zips)}");
                    }
                    return 1;
                }
            }
            else
            {
                Console.WriteLine("❌ Build directory missing!");
                return 1;
            }

            stopwatch.Stop();
            Console.WriteLine($"\n✓ SUCCESS! Total time: {stopwatch.Elapsed.TotalSeconds:F1}s");
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ PIPELINE FAILED: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            return 1;
        }
    }
}
