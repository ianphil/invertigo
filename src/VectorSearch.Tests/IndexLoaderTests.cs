using System;
using System.Collections.Generic;
using System.IO;
using ProtoBuf;
using VectorSearch.Core;
using Xunit;

namespace VectorSearch.Tests;

public class IndexLoaderTests : IDisposable
{
    private readonly string _tempFilePath;

    public IndexLoaderTests()
    {
        // Create a temporary file for testing
        _tempFilePath = Path.Combine(Path.GetTempPath(), $"test_index_{Guid.NewGuid()}.pb");
    }

    public void Dispose()
    {
        // Clean up the temporary file
        if (File.Exists(_tempFilePath))
        {
            File.Delete(_tempFilePath);
        }
    }

    [Fact]
    public void LoadIndex_ValidFile_ReturnsCorrectData()
    {
        // Arrange
        var expectedIndex = CreateSampleIndex();
        
        // Serialize the sample data to the temp file
        using (var file = File.Create(_tempFilePath))
        {
            Serializer.Serialize(file, expectedIndex);
        }

        // Act
        var loadedIndex = IndexLoader.LoadIndex(_tempFilePath);

        // Assert
        Assert.NotNull(loadedIndex);
        Assert.Equal(expectedIndex.VectorDimension, loadedIndex.VectorDimension);
        Assert.Equal(expectedIndex.CentroidCount, loadedIndex.CentroidCount);
        Assert.Equal(expectedIndex.FlattenedCentroids.Length, loadedIndex.FlattenedCentroids.Length);
        Assert.Equal(expectedIndex.Clusters.Count, loadedIndex.Clusters.Count);
        
        // Check flattened centroid values
        for (int i = 0; i < expectedIndex.FlattenedCentroids.Length; i++)
        {
            Assert.Equal(expectedIndex.FlattenedCentroids[i], loadedIndex.FlattenedCentroids[i]);
        }
        
        // Check the derived centroids 2D array is correctly reconstructed
        var expectedCentroids = expectedIndex.Centroids;
        var actualCentroids = loadedIndex.Centroids;
        
        Assert.Equal(expectedCentroids.Length, actualCentroids.Length);
        for (int i = 0; i < expectedCentroids.Length; i++)
        {
            Assert.Equal(expectedCentroids[i].Length, actualCentroids[i].Length);
            for (int j = 0; j < expectedCentroids[i].Length; j++)
            {
                Assert.Equal(expectedCentroids[i][j], actualCentroids[i][j]);
            }
        }
        
        // Check the first cluster's first vector item
        var expectedItem = expectedIndex.Clusters[0][0];
        var actualItem = loadedIndex.Clusters[0][0];
        
        Assert.Equal(expectedItem.Id, actualItem.Id);
        Assert.Equal(expectedItem.PrecomputedScore, actualItem.PrecomputedScore);
        Assert.Equal(expectedItem.Vector.Length, actualItem.Vector.Length);
        Assert.Equal(expectedItem.Metadata.Count, actualItem.Metadata.Count);
        
        // Check metadata contents
        foreach (var key in expectedItem.Metadata.Keys)
        {
            Assert.Equal(expectedItem.Metadata[key], actualItem.Metadata[key]);
        }
    }

    [Fact]
    public void LoadIndex_FileNotFound_ThrowsFileNotFoundException()
    {
        // Arrange
        string nonExistentFilePath = Path.Combine(Path.GetTempPath(), "non_existent_file.pb");

        // Act & Assert
        var exception = Assert.Throws<FileNotFoundException>(() => IndexLoader.LoadIndex(nonExistentFilePath));
        Assert.Contains("Index file not found", exception.Message);
    }

    [Fact]
    public void LoadIndex_CorruptFile_ThrowsInvalidOperationException()
    {
        // Arrange - Create a file with invalid protobuf content
        File.WriteAllText(_tempFilePath, "This is not valid protobuf data");

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => IndexLoader.LoadIndex(_tempFilePath));
        Assert.Contains("Failed to deserialize index file", exception.Message);
    }

    private static InvertedIndex CreateSampleIndex()
    {
        // Create a small sample index for testing
        var index = new InvertedIndex();
        
        // Set up centroids using the property that handles the conversion
        index.Centroids = new float[][]
        {
            new float[] { 0.1f, 0.2f, 0.3f },
            new float[] { 0.4f, 0.5f, 0.6f }
        };
        
        // Set up clusters
        index.Clusters = new Dictionary<int, List<VectorItem>>
        {
            {
                0, new List<VectorItem>
                {
                    new VectorItem
                    {
                        Id = "item1",
                        Vector = new float[] { 0.11f, 0.21f, 0.31f },
                        Metadata = new Dictionary<string, string> { { "tag", "azure" }, { "type", "document" } },
                        PrecomputedScore = 0.95f
                    },
                    new VectorItem
                    {
                        Id = "item2",
                        Vector = new float[] { 0.12f, 0.22f, 0.32f },
                        Metadata = new Dictionary<string, string> { { "tag", "azure" }, { "type", "code" } },
                        PrecomputedScore = 0.85f
                    }
                }
            },
            {
                1, new List<VectorItem>
                {
                    new VectorItem
                    {
                        Id = "item3",
                        Vector = new float[] { 0.41f, 0.51f, 0.61f },
                        Metadata = new Dictionary<string, string> { { "tag", "aws" }, { "type", "document" } },
                        PrecomputedScore = 0.75f
                    }
                }
            }
        };
        
        return index;
    }
}