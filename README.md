# Invertigo
Invertigo 🚀 flips the script on vector search! Cluster your data into centroids 🌟, zoom through high-dimensional space 🔍, and snag similar vectors in a snap ⏩. Open-source, efficient, and ready to spin your search game upside-down! 🔄✨

## 🔧 Setup Instructions

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later
- OpenAI API key (or Azure OpenAI service credentials)
- Vector index file (`.pb` format)

### Configuration
1. Clone the repository:
   ```bash
   git clone https://github.com/yourusername/invertigo.git
   cd invertigo
   ```

2. Configure your OpenAI credentials:
   
   You can either:
   - Set environment variables:
     ```bash
     export OPENAI_API_KEY="your-api-key"
     export OPENAI_ENDPOINT="https://your-azure-endpoint" # Optional for Azure OpenAI
     export OPENAI_EMBEDDING_DEPLOYMENT="text-embedding-3-small" # Default is text-embedding-3-small
     ```
   
   - Or update `appsettings.json`:
     ```json
     {
       "OpenAI": {
         "ApiKey": "your-api-key",
         "Endpoint": "https://your-azure-endpoint", // Optional
         "DeploymentName": "text-embedding-3-small" // Optional
       },
       "IndexPath": "path/to/your/index.pb"
     }
     ```

3. Set the path to your vector index file:
   ```bash
   export IndexPath="path/to/your/index.pb"
   ```
   
   The default location is `index.pb` in the application's directory.

## 🚀 Running the Application

### From the Command Line
```bash
cd src/VectorSearch.Api
dotnet run
```

The API will start on `https://localhost:7142` and `http://localhost:5000` by default.

### Using Visual Studio
1. Open the `VectorSearch.sln` solution file in Visual Studio
2. Set `VectorSearch.Api` as the startup project
3. Press F5 or click the "Run" button

## 📋 Using Swagger UI

Swagger UI is enabled in development mode to help you explore and test the API:

1. Open your browser and navigate to:
   ```
   https://localhost:7142/swagger
   ```
   
2. You'll see the available endpoints with documentation
3. Expand the `POST /api/search` endpoint
4. Click "Try it out" and enter a search request
5. Click "Execute" to test the API

## 🔍 Making API Requests

### Search API

#### Using curl
```bash
curl -X POST "https://localhost:5085/api/search" \
     -H "Content-Type: application/json" \
     -d '{
           "query": "How to connect to Azure SQL?",
           "topK": 5,
           "nProbes": 10,
           "tagFilter": "azure",
           "cosineWeight": 0.8,
           "metadataWeight": 0.2
         }'
```

#### Using REST Client
We've included HTTP files in the scripts folder that you can use with the REST Client VS Code extension:

```http
### Search for text similar to AI and vector search
POST http://localhost:5085/api/Search
Content-Type: application/json

{
  "query": "Tell me about AI and vector search",
  "topK": 3,
  "nProbes": 10,
  "cosineWeight": 0.8,
  "metadataWeight": 0.2
}
```

To use these files:
1. Install the "REST Client" extension in VS Code
2. Open the `scripts/search_api.http` file
3. Click on "Send Request" above any request to execute it

#### Request Parameters
| Parameter | Type | Description |
|-----------|------|-------------|
| query | string | The text to search for |
| topK | integer | Number of results to return |
| nProbes | integer | Number of clusters to search |
| tagFilter | string | Optional metadata filter |
| cosineWeight | float | Weight for vector similarity (0.0-1.0) |
| metadataWeight | float | Weight for metadata scoring (0.0-1.0) |

#### Response Format
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
  },
  {
    "id": "doc17",
    "metadata": {
      "tag": "azure",
      "priority": "community"
    },
    "cosineScore": 0.76,
    "metadataScore": 0.8,
    "hybridScore": 0.768
  }
]
```

### Embedding API

Generate vector embeddings for text without searching the index.

#### Using curl
```bash
curl -X POST "https://localhost:5085/api/embedding" \
     -H "Content-Type: application/json" \
     -d '{
           "text": "This is the text I want to convert to an embedding vector"
         }'
```

#### Using REST Client
We've included HTTP files in the scripts folder for testing with the REST Client VS Code extension:

```http
### Generate embedding for sample text
POST http://localhost:5085/api/Embedding
Content-Type: application/json

{
  "text": "This is a sample text that will be converted to an embedding vector. It's about artificial intelligence and vector search."
}
```

To use these files:
1. Install the "REST Client" extension in VS Code
2. Open the `scripts/embedding_api.http` file
3. Click on "Send Request" above any request to execute it

#### Request Parameters
| Parameter | Type | Description |
|-----------|------|-------------|
| text | string | The text to convert to an embedding vector |

#### Response Format
```json
{
  "embedding": [
    0.0023403291,
    -0.009303299,
    0.008032652,
    ...
    0.022048818,
    -0.023478487
  ],
  "dimensions": 1536,
  "tokenCount": null
}
```

#### Response Properties
| Property | Type | Description |
|----------|------|-------------|
| embedding | array | The embedding vector as an array of floats |
| dimensions | integer | The dimensionality of the embedding vector |
| tokenCount | integer | Token count (if available, currently null) |

## 🧪 Running Tests
```bash
cd src/VectorSearch.Tests
dotnet test
```

## 📦 Deployment

The API can be deployed to:
- Azure Web App
- Azure Container App
- Docker container
- Any hosting environment supporting .NET 8

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.
