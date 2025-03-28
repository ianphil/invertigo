using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ProtoBuf;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace VectorSearch.Core;

/// <summary>
/// Manages operations for the vector index, including adding vectors and saving to disk
/// </summary>
public class IndexManager
{
    private readonly string _indexFilePath;
    private InvertedIndex _index;
    private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
    private readonly ILogger<IndexManager>? _logger;

    /// <summary>
    /// Initializes a new instance of the IndexManager
    /// </summary>
    /// <param name="indexFilePath">Path to the index file</param>
    /// <param name="logger">Optional logger</param>
    public IndexManager(string indexFilePath, ILogger<IndexManager>? logger = null)
    {
        _indexFilePath = indexFilePath ?? throw new ArgumentNullException(nameof(indexFilePath));
        _logger = logger;
        _index = IndexLoader.LoadIndex(indexFilePath);
        _logger?.LogInformation("Initialized IndexManager with file path: {FilePath}", indexFilePath);
    }

    /// <summary>
    /// Gets the current index
    /// </summary>
    public InvertedIndex Index => _index;

    /// <summary>
    /// Adds a vector item to the index
    /// </summary>
    /// <param name="item">The vector item to add</param>
    /// <returns>True if the item was added, false otherwise</returns>
    public async Task<bool> AddItemAsync(VectorItem item)
    {
        if (item == null)
        {
            throw new ArgumentNullException(nameof(item));
        }

        await _lock.WaitAsync();
        try
        {
            _logger?.LogDebug("Adding item with ID {Id} to index", item.Id);
            
            // Initialize vector dimension if this is the first item
            if (_index.VectorDimension == 0 && item.Vector.Length > 0)
            {
                _index.VectorDimension = item.Vector.Length;
                _logger?.LogInformation("Setting index vector dimension to {Dimension}", _index.VectorDimension);
            }
            else if (_index.VectorDimension != item.Vector.Length)
            {
                _logger?.LogWarning(
                    "Vector dimension mismatch. Expected {Expected}, got {Actual}",
                    _index.VectorDimension, item.Vector.Length);
                return false;
            }

            // Simplified approach: just put the item in the first cluster
            // A more sophisticated approach would assign to the nearest centroid
            int clusterId = 0;
            
            if (_index.Clusters.Count == 0)
            {
                // Create a simple centroid for the first item
                var centroid = item.Vector;
                _index.CentroidCount = 1;
                _index.FlattenedCentroids = centroid;
            }
            
            if (!_index.Clusters.TryGetValue(clusterId, out var cluster))
            {
                cluster = new List<VectorItem>();
                _index.Clusters[clusterId] = cluster;
            }
            
            // Check if the item already exists
            var existingItem = cluster.FirstOrDefault(i => i.Id == item.Id);
            if (existingItem != null)
            {
                // Replace the existing item
                cluster.Remove(existingItem);
            }
            
            cluster.Add(item);
            
            // Save the changes to disk
            await SaveIndexAsync();
            
            _logger?.LogInformation("Successfully added item with ID {Id} to index", item.Id);
            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error adding item to index: {Message}", ex.Message);
            throw;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Saves the current index to the specified file path
    /// </summary>
    public async Task SaveIndexAsync()
    {
        try
        {
            _logger?.LogDebug("Saving index to {FilePath}", _indexFilePath);
            
            // Ensure the directory exists
            string? directory = Path.GetDirectoryName(_indexFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            // Create a temporary file to avoid corruption if saving fails
            string tempFilePath = _indexFilePath + ".tmp";
            
            using (var fileStream = new FileStream(tempFilePath, FileMode.Create))
            {
                Serializer.Serialize(fileStream, _index);
                await fileStream.FlushAsync();
            }
            
            // Replace the original file with the temp file
            if (File.Exists(_indexFilePath))
            {
                File.Delete(_indexFilePath);
            }
            
            File.Move(tempFilePath, _indexFilePath);
            
            _logger?.LogInformation("Successfully saved index with {ClusterCount} clusters to {FilePath}", 
                _index.Clusters.Count, _indexFilePath);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error saving index: {Message}", ex.Message);
            throw;
        }
    }
}