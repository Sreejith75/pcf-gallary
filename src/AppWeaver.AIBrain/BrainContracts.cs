namespace AppWeaver.AIBrain;

/// <summary>
/// Contract version constants for C# ↔ Node.js communication.
/// CRITICAL: Increment version when breaking changes are made to contracts.
/// </summary>
public static class BrainContracts
{
    /// <summary>
    /// Current contract version.
    /// Format: MAJOR.MINOR
    /// - MAJOR: Breaking changes (incompatible)
    /// - MINOR: Backward-compatible additions
    /// </summary>
    public const string Version = "1.0";

    /// <summary>
    /// Minimum supported contract version.
    /// Node.js must send a version >= this value.
    /// </summary>
    public const string MinSupportedVersion = "1.0";

    /// <summary>
    /// Validates that a contract version is supported.
    /// </summary>
    /// <param name="version">Version string to validate</param>
    /// <returns>True if supported, false otherwise</returns>
    public static bool IsVersionSupported(string? version)
    {
        if (string.IsNullOrWhiteSpace(version))
            return false;

        // Simple version comparison (MAJOR.MINOR)
        var parts = version.Split('.');
        if (parts.Length != 2)
            return false;

        if (!int.TryParse(parts[0], out var major) || !int.TryParse(parts[1], out var minor))
            return false;

        var minParts = MinSupportedVersion.Split('.');
        var minMajor = int.Parse(minParts[0]);
        var minMinor = int.Parse(minParts[1]);

        // Major version must match exactly
        if (major != minMajor)
            return false;

        // Minor version must be >= minimum
        return minor >= minMinor;
    }
}
