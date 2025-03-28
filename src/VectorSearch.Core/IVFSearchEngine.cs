using System;
using System.Collections.Generic;
using System.Linq;

namespace VectorSearch.Core;

/// <summary>
/// Represents a search result with vector item and its associated scores
/// </summary>
public class SearchResult
{
    public VectorItem Item { get; set; } = null!;
    public float CosineScore { get; set; }
    public float MetadataScore { get; set; }
    public float HybridScore { get; set; }
}

/// <summary>
/// Implements a vector search engine using the Inverted File Index (IVF) approach
/// </summary>
public class IVFSearchEngine
{
    private readonly InvertedIndex _index;
    
    /// <summary>
    /// Initializes a new instance of the IVFSearchEngine with an inverted index
    /// </summary>
    /// <param name="index">The inverted index to use for search</param>
    public IVFSearchEngine(InvertedIndex index)
    {
        _index = index ?? throw new ArgumentNullException(nameof(index));
    }
    
    /// <summary>
    /// Searches for the closest vectors to the query embedding
    /// </summary>
    /// <param name="queryEmbedding">The query embedding vector</param>
    /// <param name="topK">Maximum number of results to return</param>
    /// <param name="nProbes">Number of nearest centroids to probe</param>
    /// <param name="tagFilter">Optional filter by a specific tag value</param>
    /// <param name="cosineWeight">Weight for cosine similarity in the hybrid score</param>
    /// <param name="metadataWeight">Weight for metadata score in the hybrid score</param>
    /// <returns>A list of search results</returns>
    public List<SearchResult> Search(
        float[] queryEmbedding,
        int topK = 5,
        int nProbes = 10,
        string? tagFilter = null,
        float cosineWeight = 0.8f,
        float metadataWeight = 0.2f)
    {
        if (queryEmbedding == null || queryEmbedding.Length == 0)
        {
            throw new ArgumentException("Query embedding cannot be null or empty", nameof(queryEmbedding));
        }
        
        // Check if the index is empty
        if (_index.VectorDimension == 0 || _index.CentroidCount == 0 || _index.Clusters.Count == 0)
        {
            Console.WriteLine("Warning: Attempted to search with an empty index. Returning empty result set.");
            return new List<SearchResult>();
        }
        
        if (queryEmbedding.Length != _index.VectorDimension)
        {
            throw new ArgumentException(
                $"Query embedding dimension ({queryEmbedding.Length}) does not match index dimension ({_index.VectorDimension})",
                nameof(queryEmbedding));
        }
        
        // Ensure weights sum to 1
        float totalWeight = cosineWeight + metadataWeight;
        cosineWeight /= totalWeight;
        metadataWeight /= totalWeight;
        
        // Normalize the query vector for cosine similarity
        float[] normalizedQuery = NormalizeVector(queryEmbedding);
        
        // Find closest centroids
        var centroidDistances = new List<(int Index, float Distance)>();
        float[][] centroids = _index.Centroids;
        
        for (int i = 0; i < centroids.Length; i++)
        {
            float similarity = CosineSimilarity(normalizedQuery, NormalizeVector(centroids[i]));
            centroidDistances.Add((i, similarity));
        }
        
        // Get the nProbes closest centroids (highest cosine similarity)
        var closestCentroids = centroidDistances
            .OrderByDescending(c => c.Distance)
            .Take(Math.Min(nProbes, centroids.Length))
            .Select(c => c.Index)
            .ToList();
        
        // Collect candidates from the selected clusters
        var candidates = new List<(VectorItem Item, float CosineScore, float MetadataScore)>();
        
        foreach (int centroidIndex in closestCentroids)
        {
            if (_index.Clusters.TryGetValue(centroidIndex, out var cluster))
            {
                foreach (var item in cluster)
                {
                    // Apply tag filter if specified
                    if (!string.IsNullOrEmpty(tagFilter) && 
                        (!item.Metadata.TryGetValue("tag", out string? tag) || tag != tagFilter))
                    {
                        continue;
                    }
                    
                    float cosineScore = CosineSimilarity(normalizedQuery, NormalizeVector(item.Vector));
                    float metadataScore = item.PrecomputedScore;
                    
                    candidates.Add((item, cosineScore, metadataScore));
                }
            }
        }
        
        // Calculate hybrid scores and return top K results
        return candidates
            .Select(c => new SearchResult
            {
                Item = c.Item,
                CosineScore = c.CosineScore,
                MetadataScore = c.MetadataScore,
                HybridScore = (c.CosineScore * cosineWeight) + (c.MetadataScore * metadataWeight)
            })
            .OrderByDescending(r => r.HybridScore)
            .Take(topK)
            .ToList();
    }
    
    /// <summary>
    /// Normalizes a vector to unit length (L2 norm)
    /// </summary>
    /// <param name="vector">The vector to normalize</param>
    /// <returns>The normalized vector</returns>
    public static float[] NormalizeVector(float[] vector)
    {
        if (vector == null || vector.Length == 0)
        {
            return Array.Empty<float>();
        }
        
        // Calculate the L2 norm (Euclidean length)
        float squaredSum = vector.Sum(v => v * v);
        float magnitude = MathF.Sqrt(squaredSum);
        
        // Avoid division by zero
        if (magnitude < float.Epsilon)
        {
            return vector.Select(_ => 0f).ToArray();
        }
        
        // Create a new normalized vector
        return vector.Select(v => v / magnitude).ToArray();
    }
    
    /// <summary>
    /// Calculates the cosine similarity between two vectors
    /// </summary>
    /// <param name="v1">First vector</param>
    /// <param name="v2">Second vector</param>
    /// <returns>Cosine similarity (-1 to 1, where 1 means identical direction)</returns>
    public static float CosineSimilarity(float[] v1, float[] v2)
    {
        if (v1 == null || v2 == null || v1.Length != v2.Length || v1.Length == 0)
        {
            throw new ArgumentException("Vectors must be non-null, non-empty, and of the same length");
        }
        
        float dotProduct = 0;
        for (int i = 0; i < v1.Length; i++)
        {
            dotProduct += v1[i] * v2[i];
        }
        
        // Since we're dealing with normalized vectors, the dot product is the cosine similarity
        return dotProduct;
    }
}