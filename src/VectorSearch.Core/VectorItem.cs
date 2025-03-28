using System.Collections.Generic;
using ProtoBuf;

namespace VectorSearch.Core;

/// <summary>
/// Represents a vector item with its associated metadata and scores
/// </summary>
[ProtoContract]
public class VectorItem
{
    [ProtoMember(1)]
    public string Id { get; set; } = string.Empty;
    
    [ProtoMember(2)]
    public float[] Vector { get; set; } = Array.Empty<float>();
    
    [ProtoMember(3)]
    public Dictionary<string, string> Metadata { get; set; } = new();
    
    [ProtoMember(4)]
    public float PrecomputedScore { get; set; } = 0f;
}