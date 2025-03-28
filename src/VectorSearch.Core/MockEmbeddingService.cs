namespace VectorSearch.Core;

/// <summary>
/// Mock embedding service for testing that returns predetermined or random embeddings
/// </summary>
public class MockEmbeddingService : IEmbeddingService
{
    private readonly float[]? _fixedEmbedding;
    private readonly int _dimensions;
    private readonly Random _random;
    private readonly bool _normalize;

    /// <summary>
    /// Creates a mock embedding service that returns a fixed embedding
    /// </summary>
    /// <param name="fixedEmbedding">The fixed embedding to return for all queries</param>
    public MockEmbeddingService(float[] fixedEmbedding)
    {
        _fixedEmbedding = fixedEmbedding ?? throw new ArgumentNullException(nameof(fixedEmbedding));
        _dimensions = fixedEmbedding.Length;
        _random = new Random(42); // Fixed seed for reproducibility
        _normalize = false;
    }

    /// <summary>
    /// Creates a mock embedding service that returns random embeddings
    /// </summary>
    /// <param name="dimensions">The dimensions of the random embedding vectors</param>
    /// <param name="normalize">Whether to normalize the random vectors</param>
    /// <param name="seed">Optional seed for the random number generator</param>
    public MockEmbeddingService(int dimensions, bool normalize = true, int? seed = null)
    {
        _fixedEmbedding = null;
        _dimensions = dimensions > 0 ? dimensions : throw new ArgumentException("Dimensions must be greater than 0", nameof(dimensions));
        _random = seed.HasValue ? new Random(seed.Value) : new Random();
        _normalize = normalize;
    }

    /// <inheritdoc />
    public Task<float[]> GetEmbeddingAsync(string text)
    {
        // If we have a fixed embedding, return it
        if (_fixedEmbedding != null)
        {
            return Task.FromResult((float[])_fixedEmbedding.Clone());
        }

        // Otherwise generate a random embedding
        float[] embedding = new float[_dimensions];
        for (int i = 0; i < _dimensions; i++)
        {
            embedding[i] = (float)(_random.NextDouble() * 2 - 1); // Random values between -1 and 1
        }

        // Normalize if requested
        if (_normalize)
        {
            float magnitude = (float)Math.Sqrt(embedding.Sum(x => x * x));
            if (magnitude > 0)
            {
                for (int i = 0; i < embedding.Length; i++)
                {
                    embedding[i] /= magnitude;
                }
            }
        }

        return Task.FromResult(embedding);
    }
}