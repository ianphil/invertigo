# invertigo
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
     export OpenAI__ApiKey="your-api-key"
     export OpenAI__Endpoint="https://your-azure-endpoint" # Optional for Azure OpenAI
     export OpenAI__DeploymentName="text-embedding-3-small" # Default is text-embedding-3-small
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

### Using curl
```bash
curl -X POST "https://localhost:7142/api/search" \
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

### Using PowerShell
```powershell
$body = @{
    query = "How to connect to Azure SQL?"
    topK = 5
    nProbes = 10
    tagFilter = "azure"
    cosineWeight = 0.8
    metadataWeight = 0.2
} | ConvertTo-Json

Invoke-RestMethod -Method Post -Uri "https://localhost:7142/api/search" `
                  -ContentType "application/json" -Body $body
```

### Request Parameters
| Parameter | Type | Description |
|-----------|------|-------------|
| query | string | The text to search for |
| topK | integer | Number of results to return |
| nProbes | integer | Number of clusters to search |
| tagFilter | string | Optional metadata filter |
| cosineWeight | float | Weight for vector similarity (0.0-1.0) |
| metadataWeight | float | Weight for metadata scoring (0.0-1.0) |

### Response Format
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
