using System.Text.Json;

namespace AppWeaver.AIBrain.Logging;

/// <summary>
/// Structured logging for brain operations.
/// Logs are JSON-formatted for easy parsing and analysis.
/// </summary>
public static class BrainLogger
{
    /// <summary>
    /// Logs a brain operation.
    /// </summary>
    public static void LogOperation(
        string buildId,
        string step,
        string status,
        long durationMs,
        string? errorMessage = null,
        object? metadata = null)
    {
        var logEntry = new
        {
            timestamp = DateTimeOffset.UtcNow.ToString("o"),
            buildId,
            step,
            status,
            durationMs,
            errorMessage,
            metadata
        };

        var json = JsonSerializer.Serialize(logEntry, new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        Console.WriteLine(json);
    }

    /// <summary>
    /// Logs a validation result.
    /// </summary>
    public static void LogValidation(
        string buildId,
        string validationType,
        bool isValid,
        int totalRules,
        int passedRules,
        long durationMs)
    {
        LogOperation(
            buildId,
            $"Validate{validationType}",
            isValid ? "Passed" : "Failed",
            durationMs,
            metadata: new
            {
                totalRules,
                passedRules,
                failedRules = totalRules - passedRules
            });
    }

    /// <summary>
    /// Logs a brain task routing.
    /// </summary>
    public static void LogRouting(
        string buildId,
        string task,
        int filesLoaded,
        int estimatedTokens,
        long durationMs)
    {
        LogOperation(
            buildId,
            $"Route{task}",
            "Success",
            durationMs,
            metadata: new
            {
                filesLoaded,
                estimatedTokens
            });
    }

    /// <summary>
    /// Logs an error.
    /// </summary>
    public static void LogError(
        string buildId,
        string step,
        string errorMessage,
        Exception? exception = null)
    {
        LogOperation(
            buildId,
            step,
            "Error",
            0,
            errorMessage,
            metadata: exception != null ? new
            {
                exceptionType = exception.GetType().Name,
                stackTrace = exception.StackTrace
            } : null);
    }
}
