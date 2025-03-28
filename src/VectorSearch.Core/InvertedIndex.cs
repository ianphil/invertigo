using System.Collections.Generic;
using System.Runtime.Serialization;
using ProtoBuf;

namespace VectorSearch.Core;

/// <summary>
/// Represents an inverted index for vector search with centroids and their associated clusters
/// </summary>
[ProtoContract]
public class InvertedIndex
{
    /// <summary>
    /// Gets or sets the dimensions of each vector
    /// </summary>
    [ProtoMember(1)]
    public int VectorDimension { get; set; }

    /// <summary>
    /// Gets or sets the number of centroids
    /// </summary>
    [ProtoMember(2)]
    public int CentroidCount { get; set; }

    /// <summary>
    /// Gets or sets the flattened centroids array for serialization
    /// </summary>
    [ProtoMember(3)]
    public float[] FlattenedCentroids { get; set; } = Array.Empty<float>();

    /// <summary>
    /// Gets or sets the clusters dictionary with vector items
    /// </summary>
    [ProtoMember(4)]
    public Dictionary<int, List<VectorItem>> Clusters { get; set; } = new();

    /// <summary>
    /// Gets the centroids as a 2D array
    /// </summary>
    [IgnoreDataMember]
    public float[][] Centroids
    {
        get => UnflattenCentroids();
        set => FlattenCentroids(value);
    }

    /// <summary>
    /// Converts the flattened centroids array back to a 2D array
    /// </summary>
    private float[][] UnflattenCentroids()
    {
        if (FlattenedCentroids == null || FlattenedCentroids.Length == 0 || 
            VectorDimension == 0 || CentroidCount == 0)
        {
            return Array.Empty<float[]>();
        }

        var centroids = new float[CentroidCount][];
        
        for (int i = 0; i < CentroidCount; i++)
        {
            centroids[i] = new float[VectorDimension];
            Array.Copy(
                FlattenedCentroids, 
                i * VectorDimension, 
                centroids[i], 
                0, 
                VectorDimension
            );
        }
        
        return centroids;
    }

    /// <summary>
    /// Flattens a 2D array of centroids into a 1D array for serialization
    /// </summary>
    private void FlattenCentroids(float[][] centroids)
    {
        if (centroids == null || centroids.Length == 0)
        {
            FlattenedCentroids = Array.Empty<float>();
            VectorDimension = 0;
            CentroidCount = 0;
            return;
        }

        CentroidCount = centroids.Length;
        VectorDimension = centroids[0].Length;
        FlattenedCentroids = new float[CentroidCount * VectorDimension];

        for (int i = 0; i < centroids.Length; i++)
        {
            Array.Copy(
                centroids[i], 
                0, 
                FlattenedCentroids, 
                i * VectorDimension, 
                VectorDimension
            );
        }
    }
}