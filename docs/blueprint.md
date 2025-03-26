## 1. **High-Level Blueprint**

### 1.1 Project Structure
1. **Solution Setup**  
   - Create a new .NET 8 solution with two main projects:  
     1. `VectorSearch.Api` (ASP.NET Core Web API)  
     2. `VectorSearch.Core` (library for IVF engine, data models, etc.)  
   - Create a test project:  
     1. `VectorSearch.Tests` (xUnit or NUnit)  

2. **Core Components**  
   - **EmbeddingService**: Responsible for calling OpenAI/Azure OpenAI and retrieving embeddings.  
   - **IndexLoader**: Loads a Protobuf-based IVF index file from disk at startup.  
   - **IVFSearchEngine**:  
     - Holds the centroids.  
     - Maps cluster -> vector items.  
     - Provides a search method that returns nearest vectors, applies filters, and calculates scores.  
   - **Scoring**:  
     - Cosine similarity calculation.  
     - Metadata/hybrid scoring.  

3. **API**  
   - **SearchController** with a single `POST /api/search` endpoint.  
   - Request includes `query`, `topK`, `nProbes`, optional `tagFilter`, and weighting parameters.  
   - Response returns a list of matching documents with `id`, `metadata`, `cosineScore`, `metadataScore`, and `hybridScore`.  

4. **Test Strategy**  
   - **Unit Tests**: For IVF search logic, embedding calls (mocked), scoring, and data loading.  
   - **Integration Tests**: End-to-end calls into the `/api/search` endpoint.  
   - **Error Handling Tests**: Malformed requests, invalid credentials, missing index, etc.  

5. **Deployment**  
   - Containerize the API (Docker).  
   - Provide environment variables for OpenAI credentials, index file path, etc.  

**Outcome**: This blueprint clarifies the overall structure, ensuring we know where each piece lives.

---

## 2. **Breakdown into Iterative Chunks**

We’ll split the project into five main phases, each adding a layer of functionality.

### 2.1 Phase 1: Core Skeleton & Test Framework
- **Objective**:  
  - Set up the solution, projects, and basic test harness.  
  - Confirm that everything builds and runs.  
- **Key Steps**:  
  1. Create `.sln` file and three projects (`Api`, `Core`, `Tests`).  
  2. Configure xUnit or NUnit in `Tests`.  
  3. Add a “hello world” test to confirm the environment is set up.  

### 2.2 Phase 2: Data Models & Protobuf Index Loading
- **Objective**:  
  - Define `VectorItem` and `InvertedIndex` data structures.  
  - Implement `IndexLoader` to read from `.pb` file into memory.  
- **Key Steps**:  
  1. Add `VectorItem` and `InvertedIndex` with `[ProtoContract]` attributes.  
  2. Implement `IndexLoader` with a method like `LoadIndex(string filePath)`.  
  3. Write tests that load a small fixture `.pb` file to verify the index is read correctly.  

### 2.3 Phase 3: EmbeddingService & Mock Testing
- **Objective**:  
  - Implement an `IEmbeddingService` interface.  
  - Create a real `OpenAiEmbeddingService` and a mock version for tests.  
- **Key Steps**:  
  1. Define the interface with `Task<float[]> GetEmbeddingAsync(string text)`.  
  2. In tests, use a mock/fake that returns a known float array.  
  3. Validate that the real version calls OpenAI or Azure OpenAI endpoints.  
  4. Add error-handling tests (simulate 4xx, 5xx from the embedding API).  

### 2.4 Phase 4: IVF Engine & Cosine Similarity
- **Objective**:  
  - Implement the logic to find the nearest centroid(s) and retrieve candidate vectors.  
  - Compute cosine similarity for ranking.  
- **Key Steps**:  
  1. Implement a function to normalize vectors (L2 norm).  
  2. Implement a function to compute cosine similarity between two normalized vectors.  
  3. Implement `IVFSearchEngine` with:  
     - `Search(float[] queryEmbedding, int topK, int nProbes, string tagFilter, float cosineWeight, float metadataWeight)`.  
     - Inside, find the `nProbes` nearest centroids, gather candidate items, compute scores, apply filters, and return topK.  
  4. Test with synthetic data to ensure correct cluster selection and ranking.  

### 2.5 Phase 5: API Endpoint & Hybrid Scoring
- **Objective**:  
  - Wire everything into the `SearchController`.  
  - Add metadata scoring and combine with cosine to produce the hybrid score.  
- **Key Steps**:  
  1. Implement the `POST /api/search` endpoint.  
  2. Inside the controller:  
     - Call the embedding service.  
     - Pass embedding to the IVF engine.  
     - Return results.  
  3. Implement hybrid scoring formula:  
     `hybridScore = (cosineScore * cosineWeight) + (metadataScore * metadataWeight)`.  
  4. Write integration tests that pass various requests into the endpoint and confirm correct responses.  
  5. Ensure no orphaned code; everything is connected.  

**Outcome**: Each phase builds on the previous one in small, testable steps.

---

## 3. **Further Breakdown into Smaller Steps**

Now we’ll refine each phase into more granular tasks.

### 3.1 Phase 1: Core Skeleton & Test Framework
1. **Create Solution & Projects**  
   - `dotnet new sln -n VectorSearch`  
   - `dotnet new webapi -n VectorSearch.Api`  
   - `dotnet new classlib -n VectorSearch.Core`  
   - `dotnet new xunit -n VectorSearch.Tests`  
   - `dotnet sln add VectorSearch.Api VectorSearch.Core VectorSearch.Tests`  
2. **Add Project References**  
   - Reference `Core` from `Api`  
   - Reference `Core` from `Tests`  
3. **Verify Builds**  
   - `dotnet build`  
4. **Add a Sample Test**  
   - Simple test in `VectorSearch.Tests` that asserts `2 + 2 == 4`.  

### 3.2 Phase 2: Data Models & Protobuf Index Loading
1. **Add Protobuf NuGet Package**  
   - `Google.Protobuf` and `protobuf-net`  
2. **Create `VectorItem`**  
   - `[ProtoContract]` with fields: `Id`, `Vector`, `Metadata`, `PrecomputedScore`.  
3. **Create `InvertedIndex`**  
   - `[ProtoContract]` with fields: `Centroids`, `Clusters`.  
4. **Implement `IndexLoader`**  
   - `LoadIndex(string filePath) => InvertedIndex`  
   - Use `ProtoBuf.Serializer.Deserialize<InvertedIndex>()`.  
5. **Add Unit Tests**  
   - Mock or generate a tiny `.pb` file.  
   - Validate the loaded structure matches expected values.  

### 3.3 Phase 3: EmbeddingService & Mock Testing
1. **Define `IEmbeddingService`**  
   - `Task<float[]> GetEmbeddingAsync(string text)`.  
2. **Implement `OpenAiEmbeddingService`**  
   - Use environment variables for API key, endpoint.  
   - Return normalized float array.  
3. **Create `MockEmbeddingService`** (for tests)  
   - Return a fixed array or random array.  
4. **Add Unit Tests**  
   - Confirm `OpenAiEmbeddingService` handles success/failure paths.  
   - Confirm it normalizes outputs.  

### 3.4 Phase 4: IVF Engine & Cosine Similarity
1. **Utility Functions**  
   - `NormalizeVector(float[] vector)`  
   - `CosineSimilarity(float[] v1, float[] v2)`  
2. **Implement `IVFSearchEngine`**  
   - `ctor(InvertedIndex index)` to store index in memory.  
   - `Search(...)` method:  
     1. Normalize query embedding.  
     2. Find nearest centroids using cosine similarity.  
     3. Gather vectors in those clusters.  
     4. Compute similarity to query.  
     5. Filter by `tagFilter` if provided.  
     6. Sort by top K.  
   - `ComputeHybridScore(...)` for each vector item.  
3. **Add Tests**  
   - Use a small synthetic index.  
   - Validate that search returns correct topK items.  

### 3.5 Phase 5: API Endpoint & Hybrid Scoring
1. **Add `SearchRequest` and `SearchResult` Models**  
2. **Implement `SearchController`**  
   - `POST /api/search`  
   - Body -> `SearchRequest`  
   - Call embedding service -> query embedding.  
   - Pass to IVF engine.  
   - Return list of `SearchResult`.  
3. **Add Integration Tests**  
   - In-memory test server or `WebApplicationFactory<T>` approach.  
   - Test queries with/without `tagFilter`, different weights, etc.  
4. **Finalize**  
   - Confirm everything is wired up.  
   - No orphaned code.  
   - Confirm passing tests.  

---

## 4. **Final Series of Code-Generation Prompts**

Below are the prompts you can feed into your code-generation LLM, step by step, in a test-driven fashion. Each prompt is in a fenced code block labeled as `text`, per your request.

> **Tip**: In practice, you might combine test and implementation in a single prompt or do it in two passes: one for the test, one for the code. But here, we’ll keep them fairly compact while still encouraging TDD.

---

### 4.1 **Prompt 1**: Set Up Solution & Test Framework

```text
# Prompt 1: Create the solution, projects, and a basic test framework

Write a .NET 8 solution with:
- A Web API project named VectorSearch.Api
- A class library named VectorSearch.Core
- A test project named VectorSearch.Tests using xUnit

In VectorSearch.Tests, create a sample test that just checks if 2 + 2 = 4.

Ensure the solution builds successfully. Provide the .csproj and Program.cs files as needed. 
```

---

### 4.2 **Prompt 2**: Define Data Models & Implement Index Loader

```text
# Prompt 2: Add Protobuf-based data models and an IndexLoader

1. In VectorSearch.Core, install protobuf-net and Google.Protobuf NuGet packages.
2. Create a VectorItem class with [ProtoContract] and fields:
   - string Id
   - float[] Vector
   - Dictionary<string, string> Metadata
   - float PrecomputedScore
3. Create an InvertedIndex class with [ProtoContract] and fields:
   - float[][] Centroids
   - Dictionary<int, List<VectorItem>> Clusters
4. Create an IndexLoader class with a method LoadIndex(string filePath) returning InvertedIndex.
   - Use ProtoBuf.Serializer to deserialize from a FileStream.
5. In VectorSearch.Tests, write tests to load a small .pb file or mock the stream. 
   - Validate the loaded index matches expected data. 
6. Provide all new/updated code in the final answer.
```

---

### 4.3 **Prompt 3**: EmbeddingService & Mock Testing

```text
# Prompt 3: Create IEmbeddingService and a real OpenAiEmbeddingService plus a mock

1. Define an IEmbeddingService interface with Task<float[]> GetEmbeddingAsync(string text).
2. Implement OpenAiEmbeddingService that:
   - Reads an API key and endpoint from environment variables (e.g., OPENAI_API_KEY, OPENAI_ENDPOINT).
   - Calls OpenAI (or Azure OpenAI) to get embeddings, then normalizes them.
3. Create MockEmbeddingService for test usage, returning a fixed or random float array.
4. Add xUnit tests to verify:
   - OpenAiEmbeddingService calls the correct endpoint (you can mock HttpClient or show the code for real usage).
   - Normalization is correct.
   - MockEmbeddingService returns the expected array.
5. Provide all relevant code in the final answer.
```

---

### 4.4 **Prompt 4**: IVF Engine & Cosine Similarity

```text
# Prompt 4: Implement the IVF search engine with cosine similarity

1. In VectorSearch.Core, add utility functions:
   - float[] NormalizeVector(float[] vector)
   - float CosineSimilarity(float[] v1, float[] v2)
2. Create an IVFSearchEngine class with:
   - Constructor that accepts an InvertedIndex
   - Search(
       float[] queryEmbedding, 
       int topK, 
       int nProbes, 
       string tagFilter, 
       float cosineWeight, 
       float metadataWeight
     ) 
     that returns a list of results (e.g., (VectorItem item, float cosineScore, float metadataScore, float hybridScore)).
   - Internally, find the nProbes nearest centroids, gather candidate VectorItems, compute scores, filter by tag, and return the topK.
   - Use the formula: 
     hybridScore = (cosineScore * cosineWeight) + (metadataScore * metadataWeight)
3. In VectorSearch.Tests, add unit tests using a small synthetic InvertedIndex. Confirm:
   - The correct clusters are chosen.
   - The topK items match expected scores.
4. Provide all new/updated code in the final answer.
```

---

### 4.5 **Prompt 5**: API Endpoint & Integration Tests

```text
# Prompt 5: Add the SearchController and integrate everything

1. Create a SearchRequest model with:
   - string Query
   - int TopK
   - int NProbes
   - string TagFilter
   - float CosineWeight
   - float MetadataWeight
2. Create a SearchResult model with:
   - string Id
   - Dictionary<string, string> Metadata
   - float CosineScore
   - float MetadataScore
   - float HybridScore
3. In VectorSearch.Api, add a SearchController with POST /api/search:
   - Deserialize SearchRequest from body.
   - Call IEmbeddingService.GetEmbeddingAsync(...) to get the query vector.
   - Pass that vector to IVFSearchEngine.Search(...).
   - Map results to SearchResult and return them.
4. Add integration tests in VectorSearch.Tests that:
   - Spin up an in-memory test server or use WebApplicationFactory.
   - Test queries with/without TagFilter, different weights, etc.
   - Verify the response is correct.
5. Ensure no orphaned code. Provide final wiring and code in the answer.
```
