using System.ComponentModel.DataAnnotations;

namespace VectorSearch.Api.Models;

/// <summary>
/// Represents a request to generate embeddings for text
/// </summary>
public class EmbeddingRequest
{
    /// <summary>
    /// The text to embed
    /// </summary>
    [Required]
    public string Text { get; set; } = string.Empty;
}