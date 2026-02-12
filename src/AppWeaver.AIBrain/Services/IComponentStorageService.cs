using AppWeaver.AIBrain.Models.Specs;

namespace AppWeaver.AIBrain.Services;

public interface IComponentStorageService
{
    Task<string> StoreArtifactAsync(string buildId, string zipPath, ComponentSpec spec, CancellationToken cancellationToken = default);
    string? GetArtifactPath(string buildId);
}
