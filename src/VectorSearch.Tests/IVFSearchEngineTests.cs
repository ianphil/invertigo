using System;
using System.Collections.Generic;
using System.Linq;
using VectorSearch.Core;
using Xunit;

namespace VectorSearch.Tests;

public class IVFSearchEngineTests
{
    private InvertedIndex CreateTestIndex()
    {
        // Create a small synthetic index for testing
        var index = new InvertedIndex();
        
        // Define 3 centroids in 3D space
        index.Centroids = new float[][]
        {
            new float[] { 1.0f, 0.0f, 0.0f }, // Centroid 0 (x-axis)
            new float[] { 0.0f, 1.0f, 0.0f }, // Centroid 1 (y-axis) 
            new float[] { 0.0f, 0.0f, 1.0f }  // Centroid 2 (z-axis)
        };
        
        // Create clusters
        index.Clusters = new Dictionary<int, List<VectorItem>>
        {
            // Cluster 0 (around x-axis)
            {
                0, new List<VectorItem>
                {
                    new VectorItem
                    {
                        Id = "item1",
                        Vector = new float[] { 0.9f, 0.1f, 0.1f },
                        Metadata = new Dictionary<string, string> { { "tag", "azure" } },
                        PrecomputedScore = 1.0f
                    },
                    new VectorItem
                    {
                        Id = "item2", 
                        Vector = new float[] { 0.8f, 0.15f, 0.05f },
                        Metadata = new Dictionary<string, string> { { "tag", "ml" } },
                        PrecomputedScore = 0.5f
                    }
                }
            },
            // Cluster 1 (around y-axis)
            {
                1, new List<VectorItem>
                {
                    new VectorItem
                    {
                        Id = "item3", 
                        Vector = new float[] { 0.1f, 0.95f, 0.05f },
                        Metadata = new Dictionary<string, string> { { "tag", "azure" } },
                        PrecomputedScore = 0.7f
                    },
                    new VectorItem
                    {
                        Id = "item4", 
                        Vector = new float[] { 0.2f, 0.9f, 0.1f },
                        Metadata = new Dictionary<string, string> { { "tag", "dotnet" } },
                        PrecomputedScore = 0.8f
                    }
                }
            },
            // Cluster 2 (around z-axis)
            {
                2, new List<VectorItem>
                {
                    new VectorItem
                    {
                        Id = "item5", 
                        Vector = new float[] { 0.05f, 0.05f, 0.99f },
                        Metadata = new Dictionary<string, string> { { "tag", "azure" } },
                        PrecomputedScore = 0.9f
                    }
                }
            }
        };
        
        return index;
    }
    
    [Fact]
    public void NormalizeVector_ShouldProduceUnitVector()
    {
        // Arrange
        float[] vector = new float[] { 3.0f, 4.0f };
        
        // Act
        float[] normalized = IVFSearchEngine.NormalizeVector(vector);
        
        // Assert - length should be 1
        float length = MathF.Sqrt(normalized.Sum(v => v * v));
        Assert.Equal(1.0f, length, 3); // precision to 3 decimal places
        
        // Check individual components
        Assert.Equal(0.6f, normalized[0], 3);
        Assert.Equal(0.8f, normalized[1], 3);
    }
    
    [Fact]
    public void CosineSimilarity_ShouldCalculateCorrectly()
    {
        // Arrange
        float[] v1 = new float[] { 1.0f, 0.0f };
        float[] v2 = new float[] { 0.0f, 1.0f };
        float[] v3 = new float[] { 1.0f, 0.0f };
        
        // Act & Assert
        Assert.Equal(0.0f, IVFSearchEngine.CosineSimilarity(v1, v2), 3); // Orthogonal vectors
        Assert.Equal(1.0f, IVFSearchEngine.CosineSimilarity(v1, v3), 3); // Identical vectors
    }
    
    [Fact]
    public void Search_ShouldReturnCorrectResults()
    {
        // Arrange
        var index = CreateTestIndex();
        var engine = new IVFSearchEngine(index);
        
        // Query that should be closer to the x-axis
        float[] query = new float[] { 0.95f, 0.2f, 0.1f };
        
        // Act
        var results = engine.Search(
            queryEmbedding: query,
            topK: 3,
            nProbes: 2,
            tagFilter: null,
            cosineWeight: 0.8f,
            metadataWeight: 0.2f
        );
        
        // Assert
        Assert.Equal(3, results.Count);
        
        // The first result should be item1 (closest to our query)
        Assert.Equal("item1", results[0].Item.Id);
        
        // Check that results are properly ordered by hybrid score
        Assert.True(results[0].HybridScore >= results[1].HybridScore);
        Assert.True(results[1].HybridScore >= results[2].HybridScore);
    }
    
    [Fact]
    public void Search_WithTagFilter_ShouldFilterResults()
    {
        // Arrange
        var index = CreateTestIndex();
        var engine = new IVFSearchEngine(index);
        
        // Query
        float[] query = new float[] { 0.7f, 0.7f, 0.1f };
        
        // Act - filter by "azure" tag
        var results = engine.Search(
            queryEmbedding: query,
            topK: 5,
            nProbes: 3,
            tagFilter: "azure"
        );
        
        // Assert
        Assert.True(results.Count > 0);
        
        // All results should have the "azure" tag
        foreach (var result in results)
        {
            Assert.True(result.Item.Metadata.ContainsKey("tag"));
            Assert.Equal("azure", result.Item.Metadata["tag"]);
        }
    }
    
    [Fact]
    public void Search_WithDifferentWeights_ShouldAffectScoring()
    {
        // Arrange
        var index = CreateTestIndex();
        var engine = new IVFSearchEngine(index);
        float[] query = new float[] { 0.5f, 0.5f, 0.5f };
        
        // Act - run two searches with different weights
        var resultsCosineHeavy = engine.Search(
            queryEmbedding: query,
            cosineWeight: 1.0f,
            metadataWeight: 0.0f
        );
        
        var resultsMetadataHeavy = engine.Search(
            queryEmbedding: query,
            cosineWeight: 0.0f,
            metadataWeight: 1.0f
        );
        
        // Assert - rankings should be different
        Assert.NotEqual(resultsCosineHeavy[0].Item.Id, resultsMetadataHeavy[0].Item.Id);
        
        // Cosine-heavy search ranks by cosine score
        Assert.Equal(resultsCosineHeavy[0].CosineScore, resultsCosineHeavy[0].HybridScore);
        
        // Metadata-heavy search ranks by metadata score
        Assert.Equal(resultsMetadataHeavy[0].MetadataScore, resultsMetadataHeavy[0].HybridScore);
    }
}