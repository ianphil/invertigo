# 🧩 Vector Search API Specification

## ✅ Overview

Build a .NET Web API that supports semantic vector search using IVF (Inverted File Index) with OpenAI-powered embeddings and precomputed hybrid scoring. The API exposes a single search endpoint that:
- Embeds the input query using OpenAI
- Performs cosine-based similarity search
- Filters by metadata (optional)
- Supports hybrid scoring with metadata weight
- Returns cosine, metadata, and hybrid scores

---

## 📌 Functional Requirements

### Core Features
- Accept plain-text search queries via POST /api/search
- Convert queries to embedding vectors using OpenAI API
- Perform approximate nearest-neighbor search using IVF and cosine similarity
- Filter by metadata (e.g. tag = "azure") if provided
- Precompute and store metadata scores during indexing
- Return search results with:
  - ID and metadata
  - Cosine score
  - Metadata score
  - Hybrid score
- Allow optional tuning of cosine and metadata weights at query time

---

## 🧱 Architecture

### Components
- Web API (.NET 8)
- IVF Engine (in-memory C# library)
- OpenAI Embedding Service (Azure or OpenAI.com)
- Protobuf Index Storage (read-only at runtime)

### Data Flow
1. User sends POST /api/search with text query
2. Server embeds the query (OpenAI)
3. IVF engine loads the index and runs search
4. Matches are filtered and scored
5. Server returns ranked results

---

## 🗂️ Data Structures

### VectorItem
```csharp
[ProtoContract]
public class VectorItem
{
    [ProtoMember(1)] public string Id { get; set; }
    [ProtoMember(2)] public float[] Vector { get; set; }
    [ProtoMember(3)] public Dictionary<string, string> Metadata { get; set; } = new();
    [ProtoMember(4)] public float PrecomputedScore { get; set; } = 0f;
}
```

### InvertedIndex
```csharp
[ProtoContract]
public class InvertedIndex
{
    [ProtoMember(1)] public float[][] Centroids { get; set; }
    [ProtoMember(2)] public Dictionary<int, List<VectorItem>> Clusters { get; set; } = new();
}
```

---

## 🔒 Embedding Integration

Supports either:
- Azure OpenAI via deployment name + endpoint
- OpenAI.com via API key

Embedding call returns a normalized float[] for use in similarity scoring.

---

## 🧠 Search Logic

1. Normalize all vectors and query
2. Use cosine similarity to find nearest centroids (IVF)
3. Search vectors in those clusters
4. Filter results by metadata if applicable
5. Compute hybrid score:
   ```
   hybridScore = cosineScore * cosineWeight + metadataScore * metadataWeight
   ```

---

## 🔍 API Contract

### POST /api/search

#### Request:
```json
{
  "query": "How to connect to Azure SQL?",
  "topK": 5,
  "nProbes": 10,
  "tagFilter": "azure",
  "cosineWeight": 0.8,
  "metadataWeight": 0.2
}
```

#### Response:
```json
[
  {
    "id": "doc42",
    "metadata": {
      "tag": "azure",
      "priority": "official"
    },
    "cosineScore": 0.87,
    "metadataScore": 1.0,
    "hybridScore": 0.896
  }
]
```

---

## 📦 Data Handling

- Index is loaded into memory on startup from a .pb (protobuf) file
- Data is read-only at runtime
- Vector dimensionality must match embedding model (e.g., 1536 for OpenAI text-embedding-3-small)

---

## ⚠️ Error Handling

| Scenario | Response |
|---------|----------|
| OpenAI failure | 502 Bad Gateway with error message |
| Invalid query | 400 Bad Request |
| Vector dimension mismatch | 500 Internal Server Error |
| Index not found | 500 Internal Server Error |

---

## 🧪 Testing Plan

### Unit Tests
- Cosine similarity calculation
- K-means clustering
- Metadata scoring logic
- Normalization and distance

### Integration Tests
- Search with tag filter
- Search with default weights
- Search with overridden weights
- Search with missing fields

### Mock Tests
- Mock OpenAI client to simulate embedding failures
- Validate hybrid scoring math

---

## 🚀 Deployment Notes

- Deployable to Azure Web App or Azure Container App
- Environment variables for:
  - OpenAI key / endpoint
  - Deployment name
  - Index file path

---

## 🧠 Future Enhancements

- Batch query support
- Web-based front-end
- Index rebuilding service
- Live reloading of updated index
- Streaming OpenAI embeddings (for multi-part queries)