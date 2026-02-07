using System.Diagnostics;
using System.Text.Json;
using AppWeaver.AIBrain.Configuration;
using AppWeaver.AIBrain.Logging;
using AppWeaver.AIBrain.Models.Intent;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;

namespace AppWeaver.AIBrain.Intent;

/// <summary>
/// C# authority layer for intent interpretation.
/// Calls Node.js Intent Interpreter and validates AI output strictly.
/// CRITICAL: AI output is treated as untrusted input.
/// </summary>
public class IntentInterpreter : IIntentInterpreter
{
    private readonly BrainOptions _options;
    private readonly string _nodeExecutorPath;

    public IntentInterpreter(IOptions<BrainOptions> options)
    {
        _options = options.Value;
        _nodeExecutorPath = Path.GetFullPath(
            Path.Combine(_options.BrainRootPath, "../executor"));
    }

    /// <inheritdoc />
    public async Task<IntentInterpretationResult> InterpretAsync(
        string rawUserText,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rawUserText))
        {
            throw new ArgumentException("User text cannot be null or empty", nameof(rawUserText));
        }

        var buildId = $"intent_{DateTime.UtcNow:yyyyMMddHHmmss}";
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // STEP 1: Call Node.js Intent Interpreter
            var resultFilePath = await CallNodeJsInterpreterAsync(rawUserText, cancellationToken);

            // STEP 2: Read JSON result
            var jsonContent = await File.ReadAllTextAsync(resultFilePath, cancellationToken);
            BrainLogger.LogOperation(buildId, "InterpretIntent", "RawOutput", 0, metadata: new { content = jsonContent });

            // STEP 3: Parse JSON
            var rawResult = JsonSerializer.Deserialize<IntentInterpreterRawOutput>(
                jsonContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (rawResult == null)
            {
                throw new IntentValidationException("Failed to deserialize intent interpreter output");
            }

            // STEP 4: Validate contract (STRICT)
            ValidateContract(rawResult);

            // STEP 5: Validate confidence rules
            ValidateConfidenceRules(rawResult);

            // STEP 6: Build result
            var result = new IntentInterpretationResult
            {
                Intent = rawResult.NeedsClarification ? null : rawResult.GlobalIntent,
                Confidence = rawResult.Confidence,
                UnmappedPhrases = rawResult.UnmappedPhrases != null 
                    ? rawResult.UnmappedPhrases.AsReadOnly() 
                    : Array.Empty<string>(),
                NeedsClarification = rawResult.NeedsClarification
            };

            stopwatch.Stop();

            // STEP 7: Log result
            BrainLogger.LogOperation(
                buildId,
                "InterpretIntent",
                result.NeedsClarification ? "ClarificationRequired" : "Accepted",
                stopwatch.ElapsedMilliseconds,
                metadata: new
                {
                    confidence = result.Confidence,
                    needsClarification = result.NeedsClarification,
                    unmappedPhrasesCount = result.UnmappedPhrases.Count
                });

            return result;
        }
        catch (IntentInterpreterExecutionException)
        {
            stopwatch.Stop();
            BrainLogger.LogError(buildId, "InterpretIntent", "Node.js execution failed");
            throw;
        }
        catch (IntentValidationException)
        {
            stopwatch.Stop();
            BrainLogger.LogError(buildId, "InterpretIntent", "Validation failed");
            throw;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            BrainLogger.LogError(buildId, "InterpretIntent", ex.Message, ex);
            throw new IntentInterpreterExecutionException("Intent interpretation failed", ex);
        }
    }

    /// <summary>
    /// Calls the Node.js Intent Interpreter and returns the result file path.
    /// Supports both local process execution and remote HTTP execution (Docker).
    /// </summary>
    private async Task<string> CallNodeJsInterpreterAsync(
        string rawUserText,
        CancellationToken cancellationToken)
    {
        var executorUrl = Environment.GetEnvironmentVariable("EXECUTOR_URL");

        if (!string.IsNullOrEmpty(executorUrl))
        {
            // HTTP Mode (Docker)
            try
            {
                using var client = new HttpClient();
                // Increase timeout for AI processing
                client.Timeout = TimeSpan.FromMinutes(2);

                var payload = new
                {
                    userInput = rawUserText,
                    brainPath = _options.BrainRootPath
                };

                var response = await client.PostAsJsonAsync(
                    $"{executorUrl.TrimEnd('/')}/interpret", 
                    payload, 
                    cancellationToken);

                response.EnsureSuccessStatusCode();

                var jsonResult = await response.Content.ReadAsStringAsync(cancellationToken);
                
                // Write to temp file to match existing contract
                var outputPath = "/tmp/intent-result.json";
                await File.WriteAllTextAsync(outputPath, jsonResult, cancellationToken);
                
                return outputPath;
            }
            catch (Exception ex)
            {
                throw new IntentInterpreterExecutionException(
                    $"Remote executor failed: {ex.Message}", ex);
            }
        }

        // Local Process Mode
        var interpreterScript = Path.Combine(_nodeExecutorPath, "intent-interpreter.js");

        if (!File.Exists(interpreterScript))
        {
            throw new IntentInterpreterExecutionException(
                $"Intent interpreter script not found: {interpreterScript}");
        }

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "node",
                Arguments = $"\"{interpreterScript}\" \"{EscapeArgument(rawUserText)}\" \"{_options.BrainRootPath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        var outputBuilder = new System.Text.StringBuilder();
        var errorBuilder = new System.Text.StringBuilder();

        process.OutputDataReceived += (sender, args) =>
        {
            if (args.Data != null)
                outputBuilder.AppendLine(args.Data);
        };

        process.ErrorDataReceived += (sender, args) =>
        {
            if (args.Data != null)
                errorBuilder.AppendLine(args.Data);
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync(cancellationToken);

        var exitCode = process.ExitCode;
        var stdout = outputBuilder.ToString();
        var stderr = errorBuilder.ToString();

        // Exit code 0 = success, 1 = needs clarification (both acceptable)
        if (exitCode != 0 && exitCode != 1)
        {
            throw new IntentInterpreterExecutionException(
                $"Node.js interpreter failed with exit code {exitCode}. STDERR: {stderr}");
        }

        // Return expected file path
        return "/tmp/intent-result.json";
    }

    /// <summary>
    /// Validates the contract structure and version.
    /// CRITICAL: This enforces the trust boundary.
    /// </summary>
    private void ValidateContract(IntentInterpreterRawOutput output)
    {
        // Rule 1: Contract version must match
        if (output.Version != BrainContracts.Version)
        {
            throw new IntentValidationException(
                $"Contract version mismatch: expected {BrainContracts.Version}, got {output.Version}");
        }

        // Rule 2: All required fields must exist
        if (output.GlobalIntent == null && !output.NeedsClarification)
        {
            throw new IntentValidationException(
                "GlobalIntent is null but needsClarification is false");
        }

        // Rule 3: Confidence must be in valid range
        if (output.Confidence < 0.0 || output.Confidence > 1.0)
        {
            throw new IntentValidationException(
                $"Confidence out of range: {output.Confidence} (must be 0.0-1.0)");
        }

        // Rule 4: UnmappedPhrases must not be null
        if (output.UnmappedPhrases == null)
        {
            throw new IntentValidationException("UnmappedPhrases cannot be null");
        }
    }

    /// <summary>
    /// Validates confidence and clarification rules.
    /// CRITICAL: C# decides whether to proceed, not the AI.
    /// </summary>
    private void ValidateConfidenceRules(IntentInterpreterRawOutput output)
    {
        // Rule 1: If confidence < 0.6, needsClarification MUST be true
        if (output.Confidence < 0.6 && !output.NeedsClarification)
        {
            throw new IntentValidationException(
                $"Confidence is {output.Confidence} but needsClarification is false (must be true when confidence < 0.6)");
        }

        // Rule 2: If needsClarification is true, confidence should be < 0.6
        // (This is a warning, not a hard error, as AI might be conservative)
        if (output.NeedsClarification && output.Confidence >= 0.6)
        {
            // Log warning but don't fail
            Console.WriteLine(
                $"WARNING: needsClarification is true but confidence is {output.Confidence} (>= 0.6)");
        }
    }

    /// <summary>
    /// Escapes command-line arguments for shell execution.
    /// </summary>
    private static string EscapeArgument(string arg)
    {
        return arg.Replace("\"", "\\\"").Replace("'", "\\'");
    }
}

/// <summary>
/// Raw output from Node.js Intent Interpreter (untrusted).
/// </summary>
internal sealed class IntentInterpreterRawOutput
{
    public string? Version { get; init; }
    public GlobalIntent? GlobalIntent { get; init; }
    public double Confidence { get; init; }
    public List<string>? UnmappedPhrases { get; init; }
    public bool NeedsClarification { get; init; }
}

/// <summary>
/// Exception thrown when Node.js execution fails.
/// </summary>
public class IntentInterpreterExecutionException : Exception
{
    public IntentInterpreterExecutionException(string message)
        : base(message) { }

    public IntentInterpreterExecutionException(string message, Exception innerException)
        : base(message, innerException) { }
}

/// <summary>
/// Exception thrown when validation fails.
/// </summary>
public class IntentValidationException : Exception
{
    public IntentValidationException(string message)
        : base(message) { }
}
