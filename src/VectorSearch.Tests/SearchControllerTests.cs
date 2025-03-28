using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using VectorSearch.Api;
using VectorSearch.Api.Models;
using VectorSearch.Core;
using Xunit;

namespace VectorSearch.Tests;

public class SearchControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public SearchControllerTests(WebApplicationFactory<Program> factory)
    {
        // Create a factory with custom services for testing
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Replace real services with test versions
                services.AddSingleton<IEmbeddingService>(new MockEmbeddingService());
                services.AddSingleton(CreateTestIndex());
                services.AddSingleton<IVFSearchEngine>();
            });
        });

        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task Search_WithValidQuery_ReturnsResults()
    {
        // Arrange
        var request = new SearchRequest
        {
            Query = "test query",
            TopK = 3,
            NProbes = 5
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/search", request);

        // Assert
        response.EnsureSuccessStatusCode();
        var results = await response.Content.ReadFromJsonAsync<List<Api.Models.SearchResult>>();
        
        Assert.NotNull(results);
        Assert.Equal(3, results.Count);
        Assert.All(results, result =>
        {
            Assert.NotEmpty(result.Id);
            Assert.InRange(result.CosineScore, -1, 1); // Allow full range of cosine similarity
            // The hybrid score can combine negative cosine with positive metadata, so we don't check its range
        });
    }

    [Fact]
    public async Task Search_WithTagFilter_ReturnsFilteredResults()
    {
        // Arrange
        var request = new SearchRequest
        {
            Query = "test query",
            TopK = 10,
            TagFilter = "azure"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/search", request);

        // Assert
        response.EnsureSuccessStatusCode();
        var results = await response.Content.ReadFromJsonAsync<List<Api.Models.SearchResult>>();
        
        Assert.NotNull(results);
        Assert.All(results, result =>
        {
            Assert.Equal("azure", result.Metadata["tag"]);
        });
    }

    [Fact]
    public async Task Search_WithCustomWeights_ReturnsRankedResults()
    {
        // Arrange
        var request = new SearchRequest
        {
            Query = "test query",
            TopK = 5,
            CosineWeight = 0.3f,
            MetadataWeight = 0.7f
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/search", request);

        // Assert
        response.EnsureSuccessStatusCode();
        var results = await response.Content.ReadFromJsonAsync<List<Api.Models.SearchResult>>();
        
        Assert.NotNull(results);
        
        // Check that results are ordered by hybrid score
        for (int i = 1; i < results.Count; i++)
        {
            Assert.True(results[i - 1].HybridScore >= results[i].HybridScore);
        }
    }

    [Fact]
    public async Task Search_WithEmptyQuery_ReturnsBadRequest()
    {
        // Arrange
        var request = new SearchRequest
        {
            Query = "",
            TopK = 5
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/search", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // Helper method to create a test inverted index
    private InvertedIndex CreateTestIndex()
    {
        var index = new InvertedIndex
        {
            VectorDimension = 4
        };

        var centroids = new[]
        {
            new float[] { 1.0f, 0.0f, 0.0f, 0.0f },
            new float[] { 0.0f, 1.0f, 0.0f, 0.0f },
            new float[] { 0.0f, 0.0f, 1.0f, 0.0f }
        };
        
        index.Centroids = centroids;

        // Add items to clusters
        index.Clusters[0] = new List<VectorItem>
        {
            new VectorItem
            {
                Id = "doc1",
                Vector = new float[] { 0.9f, 0.1f, 0.0f, 0.0f },
                Metadata = new Dictionary<string, string> { { "tag", "azure" }, { "priority", "high" } },
                PrecomputedScore = 0.8f
            },
            new VectorItem
            {
                Id = "doc2",
                Vector = new float[] { 0.8f, 0.2f, 0.0f, 0.0f },
                Metadata = new Dictionary<string, string> { { "tag", "aws" }, { "priority", "medium" } },
                PrecomputedScore = 0.6f
            }
        };

        index.Clusters[1] = new List<VectorItem>
        {
            new VectorItem
            {
                Id = "doc3",
                Vector = new float[] { 0.2f, 0.8f, 0.0f, 0.0f },
                Metadata = new Dictionary<string, string> { { "tag", "azure" }, { "priority", "low" } },
                PrecomputedScore = 0.4f
            }
        };

        index.Clusters[2] = new List<VectorItem>
        {
            new VectorItem
            {
                Id = "doc4",
                Vector = new float[] { 0.0f, 0.0f, 0.9f, 0.1f },
                Metadata = new Dictionary<string, string> { { "tag", "gcp" }, { "priority", "high" } },
                PrecomputedScore = 0.9f
            },
            new VectorItem
            {
                Id = "doc5",
                Vector = new float[] { 0.1f, 0.1f, 0.7f, 0.1f },
                Metadata = new Dictionary<string, string> { { "tag", "azure" }, { "priority", "medium" } },
                PrecomputedScore = 0.7f
            }
        };

        return index;
    }
}