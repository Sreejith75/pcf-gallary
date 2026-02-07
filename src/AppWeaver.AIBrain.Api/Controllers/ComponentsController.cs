using AppWeaver.AIBrain.Api.Models;
using AppWeaver.AIBrain.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace AppWeaver.AIBrain.Api.Controllers;

/// <summary>
/// Endpoints for creating and managing PCF component builds.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ComponentsController : ControllerBase
{
    private readonly ComponentBuildService _buildService;

    public ComponentsController(ComponentBuildService buildService)
    {
        _buildService = buildService;
    }

    /// <summary>
    /// Starts a new component build based on a natural language prompt.
    /// </summary>
    /// <param name="request">The build request containing the prompt.</param>
    /// <returns>The build ID and initial status.</returns>
    /// <response code="202">Build started successfully.</response>
    /// <response code="400">Invalid request (empty prompt).</response>
    [HttpPost]
    [ProducesResponseType(typeof(ComponentBuildResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult CreateComponent([FromBody] CreateComponentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Prompt))
        {
            return BadRequest("Prompt is required.");
        }

        var buildId = _buildService.StartBuild(request.Prompt);
        
        // Construct the download URL relative to the request
        // Using "api/components/{buildId}/download"
        var downloadUrl = Url.Action(nameof(DownloadArtifact), new { buildId }) ?? $"/api/components/{buildId}/download";

        return Accepted(new ComponentBuildResponse
        {
            BuildId = buildId,
            Status = "Running",
            ZipDownloadUrl = downloadUrl
        });
    }

    /// <summary>
    /// Gets the current status of a build.
    /// </summary>
    /// <param name="buildId">The unique build identifier.</param>
    /// <returns>The build status and any error information.</returns>
    /// <response code="200">Returns the build status.</response>
    /// <response code="404">Build not found.</response>
    [HttpGet("{buildId}")]
    [ProducesResponseType(typeof(BuildStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetStatus(string buildId)
    {
        var status = _buildService.GetStatus(buildId);
        if (status == null)
        {
            return NotFound($"Build '{buildId}' not found.");
        }

        return Ok(status);
    }

    /// <summary>
    /// Downloads the resulting ZIP artifact for a completed build.
    /// </summary>
    /// <param name="buildId">The unique build identifier.</param>
    /// <returns>The ZIP file stream.</returns>
    /// <response code="200">Returns the ZIP file.</response>
    /// <response code="404">Build not found or artifact not ready.</response>
    [HttpGet("{buildId}/download")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult DownloadArtifact(string buildId)
    {
        var artifactPath = _buildService.GetArtifactPath(buildId);
        if (artifactPath == null || !System.IO.File.Exists(artifactPath))
        {
            return NotFound($"Artifact for build '{buildId}' is not available.");
        }

        var stream = new FileStream(artifactPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        var fileName = Path.GetFileName(artifactPath);
        
        return File(stream, "application/zip", fileName);
    }
}
