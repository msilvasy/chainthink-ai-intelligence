# Document AI System

A production-ready, privacy-first Document AI system built with ASP.NET Core 8, Ollama (local LLM), and Chroma DB.

## Features

- **Document Processing**: Upload and process PDF, DOCX, and TXT files
- **Intelligent Chunking**: Smart document chunking with overlap for context preservation
- **Local Embeddings**: Uses Ollama's nomic-embed-text model for privacy
- **Vector Search**: Chroma DB for fast similarity search
- **RAG Pipeline**: Retrieval Augmented Generation for grounded responses
- **Report Generation**: Structured business reports from documents

## Prerequisites

### 1. Install Ollama
Download and install from [ollama.ai](https://ollama.ai)

```bash
# Pull required models
ollama pull llama3.1:8b
ollama pull nomic-embed-text
```

### 2. Install Chroma DB (Server Mode)
```bash
Added instruction for python chromadb  in readme file
 20260507
================================================================== 
install Install Python 3.11
https://www.python.org/downloads/release/python-3119/
# Using pip
python -m pip install chromadb==0.4.24
 
#run following cmd if you encounter numpy error 
python -m pip install "numpy<2.0"
 
# Run Chroma searver
chroma run --host localhost --port 8000

=======================================================


```bash
# Using pip
pip install chromadb

# Run Chroma server
chroma run --host localhost --port 8000
```

Or using Docker:
```bash
docker run -d `
  --name chroma `
  -p 8000:8000 `
  -v chroma_data:/chroma/chroma `
  chromadb/chroma:0.4.24


## Configuration

Edit `appsettings.json` to customize:

```json
{
  "Ollama": {
    "BaseUrl": "http://localhost:11434",
    "ChatModel": "llama3.1:8b",
    "EmbeddingModel": "nomic-embed-text",
    "TimeoutSeconds": 300,
    "MaxTokens": 4096,
    "Temperature": 0.1
  },
  "Chroma": {
    "BaseUrl": "http://localhost:8000",
    "CollectionName": "documents",
    "TopK": 5,
    "TimeoutSeconds": 60
  },
  "Chunking": {
    "ChunkSize": 512,
    "ChunkOverlap": 50,
    "MinChunkSize": 100
  }
}
```

## Running the Application

```bash
dotnet run
```

Access Swagger UI at: `https://localhost:5001` (or `http://localhost:5000`)

## API Endpoints

### 1. Upload Document
`POST /api/documents/upload`

Upload a document for processing and indexing.

**Request (multipart/form-data):**
```
file: [PDF, DOCX, or TXT file]
```

**Response:**
```json
{
  "documentId": "a1b2c3d4e5f6...",
  "fileName": "quarterly_report.pdf",
  "chunkCount": 15,
  "characterCount": 12500,
  "processedAt": "2024-01-15T10:30:00Z",
  "message": "Document processed successfully in 2500ms"
}
```

### 2. Query Documents
`POST /api/documents/query`

Ask questions about uploaded documents.

**Request:**
```json
{
  "query": "What were the key revenue drivers in Q4?",
  "topK": 5,
  "documentId": null
}
```

**Response:**
```json
{
  "answer": "Based on the documents, the key revenue drivers in Q4 were: 1) Increased enterprise software subscriptions (up 23%), 2) New product launches in the Asian market, and 3) Cost optimization initiatives that improved margins by 5%. [Source 1: quarterly_report.pdf]",
  "sources": [
    {
      "documentId": "a1b2c3d4...",
      "documentName": "quarterly_report.pdf",
      "chunkId": "a1b2c3d4_chunk_3",
      "excerpt": "The Q4 results showed significant growth in enterprise software subscriptions, increasing by 23% compared to...",
      "relevanceScore": 0.89
    }
  ],
  "processingTimeMs": 1250,
  "answerFoundInContext": true
}
```

### 3. Generate Report
`POST /api/documents/report`

Generate a structured business report.

**Request:**
```json
{
  "topic": "Risk assessment for expansion into European markets",
  "topK": 10,
  "documentId": null,
  "additionalInstructions": "Focus on regulatory and competitive risks"
}
```

**Response:**
```json
{
  "topic": "Risk assessment for expansion into European markets",
  "report": {
    "executiveSummary": "The analysis of available documents indicates several key risks associated with European market expansion, primarily related to GDPR compliance requirements and established local competitors.",
    "keyFindings": [
      "GDPR compliance will require significant data infrastructure changes",
      "Three major competitors already hold 65% market share in target regions",
      "Currency exchange volatility could impact pricing strategy"
    ],
    "risks": [
      "Regulatory compliance costs estimated at $2-3M in first year",
      "Brand recognition significantly lower than local competitors",
      "Supply chain disruptions may affect initial rollout"
    ],
    "recommendations": [
      "Partner with local firms for regulatory compliance",
      "Consider acquisition strategy for market entry",
      "Develop hedging strategy for currency exposure"
    ]
  },
  "sources": [
    {
      "documentId": "b2c3d4e5...",
      "documentName": "market_analysis.docx",
      "chunkId": "b2c3d4e5_chunk_7",
      "excerpt": "European market analysis shows three dominant players controlling approximately 65% of the...",
      "relevanceScore": 0.92
    }
  ],
  "generatedAt": "2024-01-15T11:00:00Z",
  "processingTimeMs": 4500
}
```

### 4. Health Check
`GET /api/documents/health`

Check system status.

**Response:**
```json
{
  "status": "Healthy",
  "timestamp": "2024-01-15T10:00:00Z",
  "services": {
    "chromaDb": "Healthy",
    "ollama": "Healthy"
  }
}
```

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    DocumentsController                       │
├─────────────────────────────────────────────────────────────┤
│  /upload          /query              /report               │
└──────┬─────────────┬───────────────────┬────────────────────┘
       │             │                   │
       ▼             ▼                   ▼
┌──────────────┐ ┌────────────────┐ ┌────────────────┐
│DocumentParser│ │ EmbeddingService│ │   LlmService   │
│   Service    │ │    (Ollama)    │ │    (Ollama)    │
└──────┬───────┘ └───────┬────────┘ └────────────────┘
       │                 │
       ▼                 ▼
┌──────────────┐ ┌────────────────┐
│  Chunking    │ │  ChromaService │
│   Service    │ │  (Vector DB)   │
└──────────────┘ └────────────────┘
```

## RAG Flow

1. **Upload**: Document → Parse → Chunk → Embed → Store in Chroma
2. **Query**: Question → Embed → Vector Search → Build Context → LLM → Answer
3. **Report**: Topic → Embed → Vector Search → Build Context → LLM → Structured Report

## Security & Privacy

- **No cloud dependencies**: All processing happens locally
- **No data leaves your network**: Uses local Ollama and Chroma
- **Enterprise-ready**: Designed for company document processing

## Error Handling

All endpoints return consistent error responses:

```json
{
  "error": "Error type",
  "message": "Human readable message",
  "details": "Technical details (if applicable)",
  "timestamp": "2024-01-15T10:00:00Z",
  "traceId": "abc123..."
}
```

## Extending the System

### Using a Different Embedding Model
The `IEmbeddingService` interface allows swapping embedding providers:

```csharp
public interface IEmbeddingService
{
    Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken ct);
    Task<float[][]> GenerateEmbeddingsAsync(IReadOnlyList<string> texts, CancellationToken ct);
    int GetEmbeddingDimension();
}
```

### Adding New Document Types
Extend `DocumentParserService` to support additional formats:

```csharp
private async Task<string> ExtractFromNewFormatAsync(Stream stream, CancellationToken ct)
{
    // Implement extraction logic
}
```




## License

MIT License - See LICENSE file for details.





Querying ChromaDB tables (collections) from Bash is best achieved using the chromadb-cli tool, HTTP API via curl to a running server, or by wrapping Python scripts in Bash. The chromadb-cli allows listing, inspecting, and querying collections directly from the terminal. [1, 2]
Method 1: Using chromadb-cli (Recommended)
Install the CLI:
bash
pip install "chromadb[cli]"
Use code with caution.

Query/Show Collection:
bash
# List all collections
chromadb-cli list-collections

# Show data from a specific collection
chromadb-cli show-collection my_collection
Use code with caution.


Method 2: HTTP Client (Server Mode)
If running ChromaDB as a server, you can use curl to query REST endpoints. [1]
bash
# Query with query_texts
curl http://localhost:8000/api/v1/collections/<collection_id>/query \
  -X POST \
  -H "Content-Type: application/json" \
  -d '{
    "query_texts": ["search query"],
    "n_results": 5
  }'
Use code with caution.


Method 3: Python in Bash (Flexible)
For complex queries, use Bash to run inline Python scripts. [1]
bash
python3 - <<'PY'
import chromadb
client = chromadb.PersistentClient(path="./my_chroma_db")
collection = client.get_collection(name="demo")
results = collection.query(
    query_texts=["search text"],
    n_results=2
)
print(results)
PY