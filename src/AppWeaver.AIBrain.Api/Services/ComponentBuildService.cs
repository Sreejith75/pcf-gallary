using System.Collections.Concurrent;
using AppWeaver.AIBrain.Api.Models;
using AppWeaver.AIBrain.Abstractions;
using AppWeaver.AIBrain.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace AppWeaver.AIBrain.Api.Services;

/// <summary>
/// Manages async build tasks and tracks their state.
/// </summary>
public class ComponentBuildService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ConcurrentDictionary<string, BuildState> _builds = new();

    public ComponentBuildService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    /// <summary>
    /// Starts a new component build in the background.
    /// </summary>
    public string StartBuild(string prompt)
    {
        var trackingId = $"build_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid().ToString("N")[..6]}";
        
        var state = new BuildState 
        { 
            BuildId = trackingId, 
            Status = "Running", 
            CreatedAt = DateTime.UtcNow 
        };

        if (!_builds.TryAdd(trackingId, state))
        {
            throw new InvalidOperationException("Failed to generate unique build ID");
        }

        BrainLogger.LogOperation(trackingId, "ApiCreateComponent", "Started", 0);

        // Fire and forget
        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var procedure = scope.ServiceProvider.GetRequiredService<IProcedureExecutor>();

                var result = await procedure.ExecuteCreateComponentAsync(prompt);
                
                // Update state
                state.Status = "Completed";
                state.ZipPath = result.ZipPath;
                // Preview URL: /preview/{buildId}/index.html
                // Base URL is relative to API root for now, or full URL if domain known.
                // Since frontend calls API, relative is fine if proxied, but absolute is safer for direct use.
                // We'll rely on relative path from API root: /preview/buildId/index.html
                state.PreviewUrl = $"/preview/{result.BuildId}/index.html";
                
                BrainLogger.LogOperation(trackingId, "ApiCreateComponent", "Completed", 0, metadata: new { zip = result.ZipPath, internalBuildId = result.BuildId, preview = state.PreviewUrl });
            }
            catch (Exception ex)
            {
                state.Status = "Failed";
                state.Error = ex.Message;
                BrainLogger.LogError(trackingId, "ApiCreateComponent", ex.Message, ex);
            }
        });

        return trackingId;
    }

    /// <summary>
    /// Gets the status of a build.
    /// </summary>
    public BuildStatusResponse? GetStatus(string buildId)
    {
        if (_builds.TryGetValue(buildId, out var state))
        {
            return new BuildStatusResponse
            {
                BuildId = state.BuildId,
                Status = state.Status,
                PreviewUrl = state.PreviewUrl,
                Error = state.Error
            };
        }
        return null;
    }

    /// <summary>
    /// Gets the artifact path if completed.
    /// </summary>
    public string? GetArtifactPath(string buildId)
    {
        if (_builds.TryGetValue(buildId, out var state) && state.Status == "Completed")
        {
            return state.ZipPath;
        }
        return null;
    }

    private class BuildState
    {
        public required string BuildId { get; set; }
        public required string Status { get; set; } // Running, Completed, Failed
        public DateTime CreatedAt { get; set; }
        public string? ZipPath { get; set; }
        public string? PreviewUrl { get; set; }
        public string? Error { get; set; }
    }
}
