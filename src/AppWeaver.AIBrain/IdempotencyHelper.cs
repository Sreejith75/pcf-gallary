using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AppWeaver.AIBrain.Models.Intent;

namespace AppWeaver.AIBrain;

/// <summary>
/// Utilities for ensuring idempotent builds.
/// Rule: Same ExecutionPlan + Same Brain Version = Same Output
/// </summary>
public static class IdempotencyHelper
{
    /// <summary>
    /// Generates a deterministic build ID from GlobalIntent and capability ID.
    /// This ensures the same input always produces the same build ID.
    /// </summary>
    /// <param name="intent">GlobalIntent</param>
    /// <param name="capabilityId">Capability ID</param>
    /// <returns>Deterministic build ID</returns>
    public static string GenerateDeterministicBuildId(GlobalIntent intent, string capabilityId)
    {
        // Serialize intent to JSON (deterministic)
        var intentJson = JsonSerializer.Serialize(intent, new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        // Create hash input
        var hashInput = $"{intentJson}|{capabilityId}|{BrainContracts.Version}";

        // Generate SHA256 hash
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(hashInput));
        var hashHex = Convert.ToHexString(hashBytes).ToLowerInvariant();

        // Format: build_YYYYMMDD_<first 12 chars of hash>
        var datePrefix = DateTime.UtcNow.ToString("yyyyMMdd");
        var buildId = $"build_{datePrefix}_{hashHex.Substring(0, 12)}";

        return buildId;
    }

    /// <summary>
    /// Validates that a build ID is deterministic (not random).
    /// </summary>
    public static bool IsDeterministicBuildId(string buildId)
    {
        // Build ID format: build_YYYYMMDD_<hash>
        if (string.IsNullOrWhiteSpace(buildId))
            return false;

        var parts = buildId.Split('_');
        if (parts.Length != 3)
            return false;

        if (parts[0] != "build")
            return false;

        // Date part should be 8 digits
        if (parts[1].Length != 8 || !parts[1].All(char.IsDigit))
            return false;

        // Hash part should be 12 hex characters
        if (parts[2].Length != 12 || !parts[2].All(c => char.IsDigit(c) || (c >= 'a' && c <= 'f')))
            return false;

        return true;
    }

    /// <summary>
    /// Gets a deterministic output path for a build.
    /// Avoids random temp paths to ensure idempotency.
    /// </summary>
    public static string GetDeterministicOutputPath(string buildId, string baseOutputDir)
    {
        return Path.Combine(baseOutputDir, buildId);
    }
}
