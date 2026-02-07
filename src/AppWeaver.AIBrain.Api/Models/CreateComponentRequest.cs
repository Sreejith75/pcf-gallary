using System.ComponentModel.DataAnnotations;

namespace AppWeaver.AIBrain.Api.Models;

/// <summary>
/// Request to create a new component.
/// </summary>
public class CreateComponentRequest
{
    /// <summary>
    /// Natural language prompt describing the component to build.
    /// </summary>
    /// <example>Create a modern star rating control</example>
    [Required]
    public string Prompt { get; set; } = string.Empty;
}
