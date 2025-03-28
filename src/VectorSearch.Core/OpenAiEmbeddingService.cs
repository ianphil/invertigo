using Azure;
using Azure.AI.OpenAI;
using System.Numerics;
using Microsoft.Extensions.Logging;

namespace VectorSearch.Core;

/// <summary>
/// Embedding service that uses OpenAI API to convert text to embeddings
/// </summary>
public class OpenAiEmbeddingService : IEmbeddingService
{
    private readonly OpenAIClient _client;
    private readonly string _deploymentName;
    private readonly ILogger<OpenAiEmbeddingService>? _logger;

    /// <summary>
    /// Creates a new OpenAiEmbeddingService using environment variables for configuration
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when required environment variables are missing</exception>
    public OpenAiEmbeddingService()
    {
        // Get configuration from environment variables
        string? apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        string? endpoint = Environment.GetEnvironmentVariable("OPENAI_ENDPOINT");
        _deploymentName = Environment.GetEnvironmentVariable("OPENAI_EMBEDDING_DEPLOYMENT") ?? "text-embedding-3-small";

        if (string.IsNullOrEmpty(apiKey))
        {
            throw new ArgumentException("OPENAI_API_KEY environment variable is required");
        }

        // Initialize client based on whether we're using Azure OpenAI or OpenAI.com
        if (!string.IsNullOrEmpty(endpoint))
        {
            // Using Azure OpenAI
            _client = new OpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
        }
        else
        {
            // Using OpenAI.com
            _client = new OpenAIClient(apiKey);
        }
    }

    /// <summary>
    /// Creates a new OpenAiEmbeddingService with explicit configuration
    /// </summary>
    /// <param name="client">OpenAI API client</param>
    /// <param name="deploymentName">Name of the embedding deployment to use</param>
    public OpenAiEmbeddingService(OpenAIClient client, string deploymentName)
    {
        _client = client;
        _deploymentName = deploymentName;
    }

    /// <summary>
    /// Creates a new OpenAiEmbeddingService with explicit configuration
    /// </summary>
    /// <param name="apiKey">The API key for OpenAI</param>
    /// <param name="endpoint">Optional endpoint for Azure OpenAI</param>
    /// <param name="deploymentName">Name of the embedding deployment to use</param>
    /// <param name="logger">Optional logger</param>
    public OpenAiEmbeddingService(string apiKey, string? endpoint, string deploymentName, ILogger<OpenAiEmbeddingService>? logger = null)
    {
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new ArgumentException("API key cannot be empty", nameof(apiKey));
        }

        _deploymentName = deploymentName ?? "text-embedding-3-small";
        _logger = logger;

        // Initialize client based on whether we're using Azure OpenAI or OpenAI.com
        if (!string.IsNullOrEmpty(endpoint))
        {
            // Using Azure OpenAI
            _logger?.LogInformation("Initializing Azure OpenAI client with endpoint: {Endpoint}", endpoint);
            _client = new OpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
        }
        else
        {
            // Using OpenAI.com
            _logger?.LogInformation("Initializing OpenAI.com client");
            _client = new OpenAIClient(apiKey);
        }
    }

    /// <inheritdoc />
    public async Task<float[]> GetEmbeddingAsync(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("Text to embed cannot be empty", nameof(text));
        }

        _logger?.LogDebug("Getting embedding for text (length: {Length})", text.Length);
        
        try
        {
            // Make API call to get embeddings
            EmbeddingsOptions options = new EmbeddingsOptions(_deploymentName, new List<string> { text });
            Response<Embeddings> response = await _client.GetEmbeddingsAsync(options);
            
            // Get the embedding data and convert from ReadOnlyMemory<float> to float[]
            ReadOnlyMemory<float> embeddingMemory = response.Value.Data[0].Embedding;
            float[] embeddingArray = embeddingMemory.ToArray();
            
            _logger?.LogDebug("Embedding obtained with {Dimensions} dimensions", embeddingArray.Length);
            
            // Normalize the vector
            return NormalizeVector(embeddingArray);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting embedding from OpenAI");
            throw;
        }
    }

    /// <summary>
    /// Normalizes a vector to have unit length (L2 normalization)
    /// </summary>
    /// <param name="vector">Vector to normalize</param>
    /// <returns>Normalized vector</returns>
    private static float[] NormalizeVector(float[] vector)
    {
        float magnitude = (float)Math.Sqrt(vector.Sum(x => x * x));
        
        if (magnitude > 0)
        {
            for (int i = 0; i < vector.Length; i++)
            {
                vector[i] /= magnitude;
            }
        }
        
        return vector;
    }
}