namespace VectorSearch.Api.Models;

/// <summary>
/// Represents a search result with scores and metadata
/// </summary>
public class SearchResult
{
    /// <summary>
    /// Unique identifier of the result
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Metadata associated with the result
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();

    /// <summary>
    /// Cosine similarity score between the query and result
    /// </summary>
    public float CosineScore { get; set; }

    /// <summary>
    /// Precomputed metadata score
    /// </summary>
    public float MetadataScore { get; set; }

    /// <summary>
    /// Hybrid score combining cosine and metadata scores
    /// </summary>
    public float HybridScore { get; set; }
}