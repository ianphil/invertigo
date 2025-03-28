using System;
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

/// <summary>
/// Tests for the EmbeddingController
/// </summary>
public class EmbeddingControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public EmbeddingControllerTests(WebApplicationFactory<Program> factory)
    {
        // Create a factory with custom services for testing
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Use MockEmbeddingService for tests with fixed embedding vectors
                var fixedEmbedding = new float[] { 0.1f, 0.2f, 0.3f };
                services.AddSingleton<IEmbeddingService>(new MockEmbeddingService(fixedEmbedding));
            });
        });

        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GenerateEmbedding_WithValidText_ReturnsEmbedding()
    {
        // Arrange
        var request = new EmbeddingRequest
        {
            Text = "This is a test text for embedding"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/embedding", request);

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<EmbeddingResponse>();
        
        Assert.NotNull(result);
        Assert.Equal(3, result.Dimensions); // Our mock service returns 3-dimensional vectors
        Assert.NotEmpty(result.Embedding);
        Assert.Equal(0.1f, result.Embedding[0]);
        Assert.Equal(0.2f, result.Embedding[1]);
        Assert.Equal(0.3f, result.Embedding[2]);
    }

    [Fact]
    public async Task GenerateEmbedding_WithEmptyText_ReturnsBadRequest()
    {
        // Arrange
        var request = new EmbeddingRequest
        {
            Text = ""
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/embedding", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GenerateEmbedding_WithRandomizedService_ReturnsNormalizedVector()
    {
        // Create a separate factory with randomized embedding service
        var randomFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Use a randomized mock service with consistent dimension
                const int dimensions = 5;
                services.AddSingleton<IEmbeddingService>(new MockEmbeddingService(dimensions, seed: 42));
            });
        });

        var client = randomFactory.CreateClient();
        
        // Arrange
        var request = new EmbeddingRequest
        {
            Text = "Test text"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/embedding", request);

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<EmbeddingResponse>();
        
        Assert.NotNull(result);
        Assert.Equal(5, result.Dimensions);
        
        // Check that the vector is normalized (length ≈ 1.0)
        float sumOfSquares = 0;
        foreach (var val in result.Embedding)
        {
            sumOfSquares += val * val;
        }
        float magnitude = MathF.Sqrt(sumOfSquares);
        
        Assert.InRange(magnitude, 0.99f, 1.01f); // Allow for small floating-point errors
    }
}