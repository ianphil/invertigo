using System.ComponentModel.DataAnnotations;

namespace VectorSearch.Api.Models;

/// <summary>
/// Represents a search request with query parameters
/// </summary>
public class SearchRequest
{
    /// <summary>
    /// The text query to search for
    /// </summary>
    [Required]
    public string Query { get; set; } = string.Empty;

    /// <summary>
    /// Maximum number of results to return
    /// </summary>
    public int TopK { get; set; } = 5;

    /// <summary>
    /// Number of nearest centroids to probe
    /// </summary>
    public int NProbes { get; set; } = 10;

    /// <summary>
    /// Optional filter by tag value
    /// </summary>
    public string? TagFilter { get; set; }

    /// <summary>
    /// Weight for cosine similarity in the hybrid score (0-1)
    /// </summary>
    public float CosineWeight { get; set; } = 0.8f;

    /// <summary>
    /// Weight for metadata score in the hybrid score (0-1)
    /// </summary>
    public float MetadataWeight { get; set; } = 0.2f;
}