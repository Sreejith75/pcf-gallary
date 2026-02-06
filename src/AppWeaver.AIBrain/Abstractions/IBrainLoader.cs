using AppWeaver.AIBrain.Models;

namespace AppWeaver.AIBrain.Abstractions;

/// <summary>
/// Loads AI Brain artifacts from disk.
/// Responsible for file I/O and deserialization only - no business logic.
/// </summary>
public interface IBrainLoader
{
    /// <summary>
    /// Loads and deserializes a JSON file from the AI Brain directory.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to</typeparam>
    /// <param name="relativePath">Path relative to the brain root directory</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Deserialized object</returns>
    /// <exception cref="FileNotFoundException">If the file does not exist</exception>
    /// <exception cref="InvalidOperationException">If deserialization fails</exception>
    Task<T> LoadJsonAsync<T>(string relativePath, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Loads a markdown file from the AI Brain directory.
    /// </summary>
    /// <param name="relativePath">Path relative to the brain root directory</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>File content as string</returns>
    /// <exception cref="FileNotFoundException">If the file does not exist</exception>
    Task<string> LoadMarkdownAsync(string relativePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a file exists in the AI Brain directory.
    /// </summary>
    /// <param name="relativePath">Path relative to the brain root directory</param>
    /// <returns>True if file exists, false otherwise</returns>
    bool FileExists(string relativePath);

    /// <summary>
    /// Gets the absolute path to a brain artifact.
    /// </summary>
    /// <param name="relativePath">Path relative to the brain root directory</param>
    /// <returns>Absolute file path</returns>
    string GetAbsolutePath(string relativePath);
}
