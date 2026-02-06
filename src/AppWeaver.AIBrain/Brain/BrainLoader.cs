using AppWeaver.AIBrain.Abstractions;
using AppWeaver.AIBrain.Configuration;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace AppWeaver.AIBrain.Brain;

/// <summary>
/// Loads AI Brain artifacts from disk.
/// Responsible for file I/O and deserialization only - no business logic.
/// </summary>
public class BrainLoader : IBrainLoader
{
    private readonly BrainOptions _options;
    private readonly JsonSerializerOptions _jsonOptions;

    public BrainLoader(IOptions<BrainOptions> options)
    {
        _options = options.Value;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };
    }

    /// <inheritdoc />
    public async Task<T> LoadJsonAsync<T>(string relativePath, CancellationToken cancellationToken = default) where T : class
    {
        var absolutePath = GetAbsolutePath(relativePath);

        if (!File.Exists(absolutePath))
        {
            throw new FileNotFoundException($"Brain artifact not found: {relativePath}", absolutePath);
        }

        try
        {
            using var stream = File.OpenRead(absolutePath);
            var result = await JsonSerializer.DeserializeAsync<T>(stream, _jsonOptions, cancellationToken);

            if (result == null)
            {
                throw new InvalidOperationException($"Failed to deserialize {relativePath}: result was null");
            }

            return result;
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Failed to deserialize {relativePath}: {ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public async Task<string> LoadMarkdownAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        var absolutePath = GetAbsolutePath(relativePath);

        if (!File.Exists(absolutePath))
        {
            throw new FileNotFoundException($"Brain artifact not found: {relativePath}", absolutePath);
        }

        return await File.ReadAllTextAsync(absolutePath, cancellationToken);
    }

    /// <inheritdoc />
    public bool FileExists(string relativePath)
    {
        var absolutePath = GetAbsolutePath(relativePath);
        return File.Exists(absolutePath);
    }

    /// <inheritdoc />
    public string GetAbsolutePath(string relativePath)
    {
        return Path.Combine(_options.BrainRootPath, relativePath);
    }
}
