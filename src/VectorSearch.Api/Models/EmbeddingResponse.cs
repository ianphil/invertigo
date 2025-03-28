namespace VectorSearch.Api.Models;

/// <summary>
/// Represents the response from an embedding request
/// </summary>
public class EmbeddingResponse
{
    /// <summary>
    /// The embedded text as a vector of floats
    /// </summary>
    public float[] Embedding { get; set; } = Array.Empty<float>();
    
    /// <summary>
    /// The dimensionality of the embedding vector
    /// </summary>
    public int Dimensions => Embedding.Length;
    
    /// <summary>
    /// Optional token count information
    /// </summary>
    public int? TokenCount { get; set; }
    
    /// <summary>
    /// The ID of the vector item in the index (null if not persisted)
    /// </summary>
    public string? Id { get; set; }
    
    /// <summary>
    /// Whether the embedding was persisted to the index
    /// </summary>
    public bool Persisted { get; set; }
}