using System.Reflection;
using Azure;
using Azure.AI.OpenAI;
using Moq;
using VectorSearch.Core;

namespace VectorSearch.Tests;

public class EmbeddingServiceTests
{
    [Fact]
    public async Task MockEmbeddingService_Fixed_ReturnsExpectedEmbedding()
    {
        // Arrange
        float[] fixedEmbedding = { 0.1f, 0.2f, 0.3f };
        var service = new MockEmbeddingService(fixedEmbedding);
        
        // Act
        var result = await service.GetEmbeddingAsync("test");
        
        // Assert
        Assert.Equal(fixedEmbedding.Length, result.Length);
        for (int i = 0; i < fixedEmbedding.Length; i++)
        {
            Assert.Equal(fixedEmbedding[i], result[i]);
        }
    }
    
    [Fact]
    public async Task MockEmbeddingService_Random_ReturnsCorrectDimensions()
    {
        // Arrange
        const int dimensions = 1536;
        var service = new MockEmbeddingService(dimensions);
        
        // Act
        var result = await service.GetEmbeddingAsync("test");
        
        // Assert
        Assert.Equal(dimensions, result.Length);
    }
    
    [Fact]
    public async Task MockEmbeddingService_Random_IsNormalized()
    {
        // Arrange
        const int dimensions = 1536;
        var service = new MockEmbeddingService(dimensions, normalize: true);
        
        // Act
        var result = await service.GetEmbeddingAsync("test");
        
        // Assert
        float magnitude = (float)Math.Sqrt(result.Sum(x => x * x));
        Assert.True(Math.Abs(magnitude - 1.0f) < 0.00001f); // Check that magnitude is approximately 1
    }
    
    [Fact]
    public async Task MockEmbeddingService_WithSeed_ReturnsDeterministicResults()
    {
        // Arrange
        const int dimensions = 10;
        const int seed = 42;
        var service1 = new MockEmbeddingService(dimensions, true, seed);
        var service2 = new MockEmbeddingService(dimensions, true, seed);
        
        // Act
        var result1 = await service1.GetEmbeddingAsync("test");
        var result2 = await service2.GetEmbeddingAsync("test");
        
        // Assert
        for (int i = 0; i < dimensions; i++)
        {
            Assert.Equal(result1[i], result2[i]);
        }
    }
    
    [Fact]
    public async Task OpenAiEmbeddingService_GetEmbedding_CallsCorrectEndpoint()
    {
        // Arrange
        var mockClient = new Mock<OpenAIClient>();
        const string deploymentName = "test-embedding-model";
        
        // Create a mock Response<Embeddings>
        var mockResponse = new Mock<Response<Embeddings>>();
        
        // Mock the GetEmbeddingsAsync method to verify it's called with correct parameters
        mockClient
            .Setup(c => c.GetEmbeddingsAsync(
                It.Is<EmbeddingsOptions>(o => 
                    o.DeploymentName == deploymentName && 
                    o.Input.Count == 1 && 
                    o.Input[0] == "Test text"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockResponse.Object);
            
        // Create a service with our mocked client
        var service = new OpenAiEmbeddingService(mockClient.Object, deploymentName);
        
        // Act - this will throw an exception because we haven't fully mocked the response
        // but that's OK - we just want to verify the client is called correctly
        try
        {
            await service.GetEmbeddingAsync("Test text");
        }
        catch (NullReferenceException)
        {
            // Expected because we haven't fully mocked the response chain
        }
        
        // Assert
        mockClient.Verify(c => c.GetEmbeddingsAsync(
            It.Is<EmbeddingsOptions>(o => 
                o.DeploymentName == deploymentName && 
                o.Input.Count == 1 && 
                o.Input[0] == "Test text"),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }
    
    [Fact]
    public void NormalizeVector_NormalizesCorrectly()
    {
        // Since we can't easily mock the OpenAI embedding response fully,
        // we'll test the normalization logic directly using reflection to 
        // access the private NormalizeVector method
        
        // Arrange
        var service = new OpenAiEmbeddingService(new Mock<OpenAIClient>().Object, "test");
        
        // Create a vector that needs normalization
        float[] vector = { 1.0f, 2.0f, 2.0f };  // This has magnitude of 3
        
        // Use reflection to access the private NormalizeVector method
        var normalizeMethod = typeof(OpenAiEmbeddingService)
            .GetMethod("NormalizeVector", BindingFlags.NonPublic | BindingFlags.Static);
        
        // Act
        var normalizedVector = (float[])normalizeMethod.Invoke(null, new object[] { (float[])vector.Clone() });
        
        // Assert
        float magnitude = (float)Math.Sqrt(normalizedVector.Sum(x => x * x));
        Assert.True(Math.Abs(magnitude - 1.0f) < 0.00001f); // Check that magnitude is approximately 1
        
        // Check expected normalized values (1/3, 2/3, 2/3)
        float expectedValue1 = 1.0f / 3.0f;
        float expectedValue2 = 2.0f / 3.0f;
        
        Assert.True(Math.Abs(normalizedVector[0] - expectedValue1) < 0.00001f);
        Assert.True(Math.Abs(normalizedVector[1] - expectedValue2) < 0.00001f);
        Assert.True(Math.Abs(normalizedVector[2] - expectedValue2) < 0.00001f);
    }
    
    [Fact]
    public async Task OpenAiEmbeddingService_ThrowsWhenTextIsEmpty()
    {
        // Arrange
        var mockClient = new Mock<OpenAIClient>();
        var service = new OpenAiEmbeddingService(mockClient.Object, "test-model");
        
        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => service.GetEmbeddingAsync(""));
        Assert.Contains("Text to embed cannot be empty", exception.Message);
    }
}