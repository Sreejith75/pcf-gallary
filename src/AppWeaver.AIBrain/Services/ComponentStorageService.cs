using System.Text.Json;
using AppWeaver.AIBrain.Configuration;
using AppWeaver.AIBrain.Logging;
using AppWeaver.AIBrain.Models.Specs;
using Microsoft.Extensions.Options;

namespace AppWeaver.AIBrain.Services;

public class ComponentStorageService : IComponentStorageService
{
    private readonly BrainOptions _options;
    private readonly string _storageRoot;

    public ComponentStorageService(IOptions<BrainOptions> options)
    {
        _options = options.Value;
        // Use a persistent location, e.g., data/components
        _storageRoot = Path.Combine(_options.BrainRootPath, "../data/components");
        if (!Directory.Exists(_storageRoot))
        {
            Directory.CreateDirectory(_storageRoot);
        }
    }

    public async Task<string> StoreArtifactAsync(string buildId, string zipPath, ComponentSpec spec, CancellationToken cancellationToken = default)
    {
        try
        {
            // Structure: data/components/{componentId}/{buildId}/
            // OR: data/components/{buildId}/ to keep it simple and aligned with build IDs
            // Let's use buildId as primary key for now, maybe alias by componentId later.
            
            var targetDir = Path.Combine(_storageRoot, buildId);
            Directory.CreateDirectory(targetDir);

            var fileName = Path.GetFileName(zipPath);
            var targetPath = Path.Combine(targetDir, fileName);

            // Copy ZIP
            File.Copy(zipPath, targetPath, overwrite: true);

            // Save Metadata
            var metadataPath = Path.Combine(targetDir, "metadata.json");
            var metadata = new
            {
                id = buildId,
                componentId = spec.ComponentId,
                componentName = spec.ComponentName,
                version = spec.Version,
                createdAt = DateTime.UtcNow,
                artifactName = fileName
            };
            
            await File.WriteAllTextAsync(metadataPath, JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true }), cancellationToken);

            BrainLogger.LogOperation(buildId, "Storage", "Stored", 0, metadata: new { path = targetPath });

            return targetPath;
        }
        catch (Exception ex)
        {
            BrainLogger.LogError(buildId, "Storage", "Failed to store artifact", ex);
            throw;
        }
    }

    public string? GetArtifactPath(string buildId)
    {
        var targetDir = Path.Combine(_storageRoot, buildId);
        if (!Directory.Exists(targetDir)) return null;

        // Find .zip file
        var zipFile = Directory.GetFiles(targetDir, "*.zip").FirstOrDefault();
        return zipFile;
    }
}
