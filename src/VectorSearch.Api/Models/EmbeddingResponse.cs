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
}