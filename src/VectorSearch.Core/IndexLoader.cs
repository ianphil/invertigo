using System;
using System.IO;
using ProtoBuf;

namespace VectorSearch.Core;

/// <summary>
/// Provides functionality to load inverted index data from protobuf files
/// </summary>
public class IndexLoader
{
    /// <summary>
    /// Loads an InvertedIndex from a protobuf file
    /// </summary>
    /// <param name="filePath">Path to the .pb file containing the serialized index</param>
    /// <returns>The deserialized InvertedIndex</returns>
    /// <exception cref="FileNotFoundException">Thrown when the specified file cannot be found</exception>
    /// <exception cref="InvalidOperationException">Thrown when the file cannot be deserialized</exception>
    public static InvertedIndex LoadIndex(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Index file not found at path: {filePath}");
        }

        try
        {
            using FileStream stream = File.OpenRead(filePath);
            return Serializer.Deserialize<InvertedIndex>(stream);
        }
        catch (Exception ex) when (ex is not FileNotFoundException)
        {
            throw new InvalidOperationException($"Failed to deserialize index file: {ex.Message}", ex);
        }
    }
}