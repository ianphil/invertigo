namespace VectorSearch.Core;

/// <summary>
/// Interface for embedding services that convert text to vector embeddings
/// </summary>
public interface IEmbeddingService
{
    /// <summary>
    /// Converts a text string to a normalized embedding vector
    /// </summary>
    /// <param name="text">The text to convert to an embedding</param>
    /// <returns>A normalized float array representing the embedding vector</returns>
    Task<float[]> GetEmbeddingAsync(string text);
}