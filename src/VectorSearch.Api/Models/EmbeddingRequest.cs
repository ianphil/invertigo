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

    /// <summary>
    /// Unique identifier for the document/item (optional, will be auto-generated if not provided)
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// Optional metadata to store with the embedding
    /// </summary>
    public Dictionary<string, string>? Metadata { get; set; }
    
    /// <summary>
    /// Optional score for ranking (defaults to 0)
    /// </summary>
    public float Score { get; set; } = 0f;
    
    /// <summary>
    /// Whether to persist this embedding to the index (defaults to true)
    /// </summary>
    public bool Persist { get; set; } = true;
}