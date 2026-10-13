using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using DocumentAI.Configuration;
using DocumentAI.DTOs.Responses;
using DocumentAI.Models;
using DocumentAI.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace DocumentAI.Services;

public sealed class ChromaService : IChromaService
{
    private readonly HttpClient _httpClient;
    private readonly ChromaSettings _settings;
    private readonly IEmbeddingService _embeddingService;
    private readonly IBm25Service _bm25Service;
    private readonly ILogger<ChromaService> _logger;
    private string? _collectionId;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public ChromaService(
        HttpClient httpClient,
        IOptions<ChromaSettings> settings,
        IEmbeddingService embeddingService,
        IBm25Service bm25Service,
        ILogger<ChromaService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _embeddingService = embeddingService;
        _bm25Service = bm25Service;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Initializing Chroma collection: {CollectionName}", _settings.CollectionName);

        // Try to get existing collection by name
        var getResponse = await _httpClient.GetAsync(
            $"{_settings.BaseUrl}/api/v1/collections/{_settings.CollectionName}",
            cancellationToken);

        if (getResponse.IsSuccessStatusCode)
        {
            var collection = await getResponse.Content.ReadFromJsonAsync<ChromaCollection>(JsonOptions, cancellationToken);
            _collectionId = collection?.Id ?? collection?.Name;
            _logger.LogInformation("Found existing collection with ID: {CollectionId}", _collectionId);
            await LogCollectionDiagnosticsAsync(collection, cancellationToken);
            
            // If ID is null but name exists, use name
            if (string.IsNullOrEmpty(_collectionId))
            {
                _collectionId = _settings.CollectionName;
            }
            return;
        }

        var collectionMetadata = new Dictionary<string, object>
        {
            ["hnsw:space"] = _settings.DistanceMetric
        };

        // Try different collection creation formats
        var createRequests = new[]
        {
            // Format 1: With get_or_create and collection metadata
            new Dictionary<string, object>
            {
                ["name"] = _settings.CollectionName,
                ["metadata"] = collectionMetadata,
                ["get_or_create"] = true
            },
            // Format 2: With metadata
            new Dictionary<string, object>
            {
                ["name"] = _settings.CollectionName,
                ["metadata"] = collectionMetadata,
                ["get_or_create"] = true
            },
            // Format 3: Without get_or_create flag
            new Dictionary<string, object>
            {
                ["name"] = _settings.CollectionName,
                ["metadata"] = collectionMetadata
            },
            // Format 4: Using snake_case
            new Dictionary<string, object>
            {
                ["name"] = _settings.CollectionName,
                ["get_or_create"] = true,
                ["metadata"] = collectionMetadata
            }
        };

        HttpResponseMessage? createResponse = null;
        Dictionary<string, object>? workingRequest = null;

        foreach (var createRequest in createRequests)
        {
            _logger.LogDebug("Trying collection creation with format containing {KeyCount} keys", createRequest.Count);
            
            createResponse = await _httpClient.PostAsJsonAsync(
                $"{_settings.BaseUrl}/api/v1/collections",
                createRequest,
                JsonOptions,
                cancellationToken);

            if (createResponse.IsSuccessStatusCode)
            {
                workingRequest = createRequest;
                _logger.LogInformation("Collection creation succeeded with format");
                break;
            }
            
            var errorContent = await createResponse.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("Collection creation failed. Status: {StatusCode}, Response: {ErrorContent}", 
                createResponse.StatusCode, errorContent);
        }

        if (createResponse == null || !createResponse.IsSuccessStatusCode)
        {
            var errorContent = createResponse != null 
                ? await createResponse.Content.ReadAsStringAsync(cancellationToken)
                : "No response received";
            
            _logger.LogError("All collection creation formats failed. Last Status: {StatusCode}, Response: {ErrorContent}", 
                createResponse?.StatusCode, errorContent);
            
            // If all formats fail, try to use the collection name as ID and continue
            // (collection might already exist or be created differently)
            _logger.LogWarning("Falling back to using collection name as ID");
            _collectionId = _settings.CollectionName;
            return;
        }

        var newCollection = await createResponse.Content.ReadFromJsonAsync<ChromaCollection>(JsonOptions, cancellationToken);
        _collectionId = newCollection?.Id ?? newCollection?.Name ?? _settings.CollectionName;
        _logger.LogInformation("Created/retrieved collection with ID: {CollectionId}", _collectionId);
        await LogCollectionDiagnosticsAsync(newCollection, cancellationToken);
    }

    public async Task StoreChunksAsync(string userId,IReadOnlyList<DocumentChunk> chunks, float[][] embeddings, CancellationToken cancellationToken = default)
    {
        if (chunks.Count == 0)
            return;

        if (chunks.Count != embeddings.Length)
            throw new ArgumentException("Number of chunks must match number of embeddings");

        await EnsureCollectionExistsAsync(cancellationToken);

        if (string.IsNullOrEmpty(_collectionId))
        {
            throw new InvalidOperationException("Collection ID is not available. Collection initialization failed.");
        }

        _logger.LogInformation("Storing {Count} chunks in Chroma (Collection ID: {CollectionId})", chunks.Count, _collectionId);

        var request = new
        {
            ids = chunks.Select(c => c.Id).ToList(),
            embeddings = embeddings.Select(e => e.ToList()).ToList(),
            documents = chunks.Select(c => c.Content).ToList(),
            metadatas = chunks.Select(c => new Dictionary<string, object>
            {
                ["document_id"] = c.DocumentId,
                ["user_id"] = userId,
                ["document_name"] = c.DocumentName,
                ["chunk_index"] = c.ChunkIndex,
                ["start_position"] = c.StartPosition,
                ["end_position"] = c.EndPosition
            }).ToList()
        };

        // Try different endpoint variations
        var endpoints = new[]
        {
            $"{_settings.BaseUrl}/api/v1/collections/{_collectionId}/add",
            $"{_settings.BaseUrl}/api/v1/collections/{_collectionId}/upsert",
            $"{_settings.BaseUrl}/api/v1/collections/{_settings.CollectionName}/add",
            $"{_settings.BaseUrl}/api/v1/collections/{_settings.CollectionName}/upsert"
        };

        HttpResponseMessage? response = null;
        string? workingEndpoint = null;

        foreach (var endpoint in endpoints)
        {
            _logger.LogDebug("Trying endpoint: {Endpoint}", endpoint);
            response = await _httpClient.PostAsJsonAsync(endpoint, request, JsonOptions, cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                workingEndpoint = endpoint;
                _logger.LogInformation("Successfully used endpoint: {Endpoint}", endpoint);
                break;
            }
            
            if (response.StatusCode != System.Net.HttpStatusCode.MethodNotAllowed)
            {
                // If it's not a 405, this might be the right endpoint but with a different error
                workingEndpoint = endpoint;
                break;
            }
        }

        if (response == null || workingEndpoint == null)
        {
            throw new HttpRequestException("All endpoint variations failed with 405 Method Not Allowed");
        }

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Failed to store chunks. Endpoint: {Endpoint}, Status: {StatusCode}, Response: {ErrorContent}", 
                workingEndpoint, response.StatusCode, errorContent);
            throw new HttpRequestException($"Failed to store chunks in Chroma DB. Status: {response.StatusCode}, Response: {errorContent}");
        }

        _logger.LogInformation("Successfully stored {Count} chunks", chunks.Count);
        await _bm25Service.IndexChunksAsync(userId, chunks, cancellationToken);
    }

    public async Task<IReadOnlyList<RetrievedContext>> QueryAsync(string userId,
        float[] queryEmbedding, 
        int topK, 
        string? documentId = null,
        IReadOnlyList<string>? documentIds = null,
        CancellationToken cancellationToken = default)
    {
        await EnsureCollectionExistsAsync(cancellationToken);

        var request = new Dictionary<string, object>
        {
            ["query_embeddings"] = new[] { queryEmbedding.ToList() },
            ["n_results"] = topK,
            ["include"] = new[] { "documents", "metadatas", "distances" }
        };

        var filters = new List<object>
        {
            new Dictionary<string, object> { ["user_id"] = userId }
        };

        // Handle multiple document IDs (takes precedence over single documentId)
        if (documentIds != null && documentIds.Count > 0)
        {
            filters.Add(new Dictionary<string, object>
            {
                ["document_id"] = new Dictionary<string, object>
                {
                    ["$in"] = documentIds.ToList()
                }
            });
        }
        else if (!string.IsNullOrEmpty(documentId))
        {
            filters.Add(new Dictionary<string, object>
            {
                ["document_id"] = documentId
            });
        }
        request["where"] = filters.Count == 1 ? filters[0] : new Dictionary<string, object> { ["$and"] = filters };

        if (string.IsNullOrEmpty(_collectionId))
        {
            throw new InvalidOperationException("Collection ID is not available. Collection initialization failed.");
        }

        // Try using collection ID first
        var endpoint = $"{_settings.BaseUrl}/api/v1/collections/{_collectionId}/query";
        var response = await _httpClient.PostAsJsonAsync(endpoint, request, JsonOptions, cancellationToken);

        // If that fails with 405, try using collection name
        if (response.StatusCode == System.Net.HttpStatusCode.MethodNotAllowed)
        {
            endpoint = $"{_settings.BaseUrl}/api/v1/collections/{_settings.CollectionName}/query";
            response = await _httpClient.PostAsJsonAsync(endpoint, request, JsonOptions, cancellationToken);
        }

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ChromaQueryResponse>(JsonOptions, cancellationToken);

        if (result?.Ids == null || result.Ids.Count == 0 || result.Ids[0].Count == 0)
        {
            _logger.LogInformation("No results found for query");
            return [];
        }

        var contexts = new List<RetrievedContext>();
        var ids = result.Ids[0];
        var documents = result.Documents?[0] ?? [];
        var metadatas = result.Metadatas?[0] ?? [];
        var distances = result.Distances?[0] ?? [];

        for (int i = 0; i < ids.Count; i++)
        {
            var metadata = i < metadatas.Count ? metadatas[i] : new Dictionary<string, JsonElement>();
            var distance = i < distances.Count ? distances[i] : 0f;
            float? similarity = string.Equals(_settings.DistanceMetric, "cosine", StringComparison.OrdinalIgnoreCase)
                ? Math.Clamp(1f - distance, 0f, 1f)
                : null;
            double? displayScore = similarity.HasValue
                ? Math.Round(similarity.Value * 100, 1)
                : null;

            _logger.LogDebug(
                "Retrieved chunk {ChunkId}. Distance={Distance}, Similarity={Similarity}",
                ids[i],
                distance,
                similarity);
            
            contexts.Add(new RetrievedContext
            {
                ChunkId = ids[i],
                DocumentId = GetMetadataString(metadata, "document_id"),
                DocumentName = GetMetadataString(metadata, "document_name"),
                Content = i < documents.Count ? documents[i] : string.Empty,
                Distance = distance,
                Similarity = similarity,
                DisplayScore = displayScore,
                Metadata = metadata.ToDictionary(
                    kvp => kvp.Key, 
                    kvp => kvp.Value.ToString() ?? string.Empty)
            });
        }

        _logger.LogInformation("Retrieved {Count} contexts for query", contexts.Count);
        return contexts;
    }

    public async Task DeleteDocumentAsync(string documentId, CancellationToken cancellationToken = default)
    {
        await EnsureCollectionExistsAsync(cancellationToken);

        var request = new
        {
            where = new Dictionary<string, object>
            {
                ["document_id"] = documentId
            }
        };

        if (string.IsNullOrEmpty(_collectionId))
        {
            throw new InvalidOperationException("Collection ID is not available. Collection initialization failed.");
        }

        // Try using collection ID first
        var endpoint = $"{_settings.BaseUrl}/api/v1/collections/{_collectionId}/delete";
        var response = await _httpClient.PostAsJsonAsync(endpoint, request, JsonOptions, cancellationToken);

        // If that fails with 405, try using collection name
        if (response.StatusCode == System.Net.HttpStatusCode.MethodNotAllowed)
        {
            endpoint = $"{_settings.BaseUrl}/api/v1/collections/{_settings.CollectionName}/delete";
            response = await _httpClient.PostAsJsonAsync(endpoint, request, JsonOptions, cancellationToken);
        }

        response.EnsureSuccessStatusCode();
        _logger.LogInformation("Deleted all chunks for document {DocumentId}", documentId);
        await _bm25Service.DeleteDocumentAsync(documentId, cancellationToken);
    }

    public async Task<bool> HealthCheckAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_settings.BaseUrl}/api/v1/heartbeat", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Chroma health check failed");
            return false;
        }
    }

    public async Task<IReadOnlyList<EmbeddedDocumentResponse>> GetEmbeddedDocumentsAsync(string userId, CancellationToken cancellationToken = default)
    {
        await EnsureCollectionExistsAsync(cancellationToken);

        if (string.IsNullOrEmpty(_collectionId))
        {
            throw new InvalidOperationException("Collection ID is not available. Collection initialization failed.");
        }

        // Get all documents from the collection
        // Chroma requires at least one of: ids, where, or limit
        // Using a high limit to get all documents
        var request = new Dictionary<string, object>
        {
            ["where"] = new Dictionary<string, object>()
            {
                ["user_id"] = userId
            }
            ,
            ["include"] = new[] { "documents", "metadatas" }
        };

        var endpoint = $"{_settings.BaseUrl}/api/v1/collections/{_collectionId}/get";

        var response = await _httpClient.PostAsJsonAsync(
            endpoint,
            request,
            JsonOptions,
            cancellationToken
        );



        if (response.StatusCode == System.Net.HttpStatusCode.MethodNotAllowed)
        {
            endpoint = $"{_settings.BaseUrl}/api/v1/collections/{_settings.CollectionName}/get";
            response = await _httpClient.PostAsJsonAsync(endpoint, request, JsonOptions, cancellationToken);
        }

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Failed to get embedded documents. Status: {StatusCode}, Response: {ErrorContent}", 
                response.StatusCode, errorContent);
            throw new HttpRequestException($"Failed to get embedded documents from Chroma DB. Status: {response.StatusCode}, Response: {errorContent}");
        }

        var result = await response.Content.ReadFromJsonAsync<ChromaGetResponse>(JsonOptions, cancellationToken);

        if (result?.Ids == null || result.Ids.Count == 0)
        {
            _logger.LogInformation("No documents found in collection");
            return [];
        }

        // Group by documentId to get distinct documents
        var documentMap = new Dictionary<string, EmbeddedDocumentResponse>();

        for (int i = 0; i < result.Ids.Count; i++)
        {
            var metadata = i < result.Metadatas?.Count ? result.Metadatas[i] : new Dictionary<string, JsonElement>();
            var documentId = GetMetadataString(metadata, "document_id");
            // userId = GetMetadataString(metadata, "user_id");
            var documentName = GetMetadataString(metadata, "document_name");
            var uploadedAtStr = GetMetadataString(metadata, "uploaded_at") 
                ?? GetMetadataString(metadata, "uploadedAt");

            if (string.IsNullOrEmpty(documentId))
                continue;

            if (documentMap.TryGetValue(documentId, out var existing))
            {
                // Increment chunk count
                documentMap[documentId] = new EmbeddedDocumentResponse
                {
                    UserId=existing.UserId,
                    DocumentId = existing.DocumentId,
                    DocumentName = existing.DocumentName,
                    ChunkCount = existing.ChunkCount + 1,
                    UploadedAt = existing.UploadedAt
                };
            }
            else
            {
                DateTime uploadedAt;
                if (!string.IsNullOrEmpty(uploadedAtStr) && DateTime.TryParse(uploadedAtStr, out var parsedDate))
                {
                    uploadedAt = parsedDate;
                }
                else
                {
                    // Fallback to current time if not available
                    uploadedAt = DateTime.UtcNow;
                }

                documentMap[documentId] = new EmbeddedDocumentResponse
                {
                    UserId= userId,
                    DocumentId = documentId,
                    DocumentName = documentName,
                    ChunkCount = 1,
                    UploadedAt = uploadedAt
                };
            }
        }

        _logger.LogInformation("Retrieved {Count} distinct documents", documentMap.Count);
        return documentMap.Values.ToList();
    }

    public async Task<IReadOnlyList<EmbeddedChunkResponse>> GetDocumentChunksAsync(string userId,string documentId, CancellationToken cancellationToken = default)
    {
        await EnsureCollectionExistsAsync(cancellationToken);

        if (string.IsNullOrEmpty(_collectionId))
        {
            throw new InvalidOperationException("Collection ID is not available. Collection initialization failed.");
        }



        var request = new Dictionary<string, object>
        {
            ["where"] = new Dictionary<string, object> // ✅ METADATA FILTER
            {
                ["documentId"] = documentId              // ✅ CORRECT KEY
            },
            ["include"] = new[]                         // ✅ VALID INCLUDES ONLY
        {
            "documents",
            "metadatas",
            "embeddings"
        }
        };

        var endpoint = $"{_settings.BaseUrl}/api/v1/collections/{_collectionId}/get";
        var response = await _httpClient.PostAsJsonAsync(endpoint, request, JsonOptions, cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.MethodNotAllowed)
        {
            endpoint = $"{_settings.BaseUrl}/api/v1/collections/{_settings.CollectionName}/get";
            response = await _httpClient.PostAsJsonAsync(endpoint, request, JsonOptions, cancellationToken);
        }

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ChromaGetResponse>(JsonOptions, cancellationToken);

        if (result?.Ids == null || result.Ids.Count == 0)
        {
            _logger.LogInformation("No chunks found for document {DocumentId}", documentId);
            return [];
        }

        var chunks = new List<EmbeddedChunkResponse>();

        for (int i = 0; i < result.Ids.Count; i++)
        {
            var chunkId = result.Ids[i];
            var document = i < result.Documents?.Count ? result.Documents[i] : string.Empty;
            var embedding = result.Embeddings != null && i < result.Embeddings.Count ? result.Embeddings[i] : null;

            var previewText = document.Length > 200
                ? document[..200] + "..."
                : document;

            var vectorSize = embedding?.Count ?? 0;

            chunks.Add(new EmbeddedChunkResponse
            {
                ChunkId = chunkId,
                PreviewText = previewText,
                EmbeddingVectorSize = vectorSize
            });
        }

        _logger.LogInformation("Retrieved {Count} chunks for document {DocumentId}", chunks.Count, documentId);
        return chunks;
    }

    public async Task DeleteChunkAsync(string chunkId, CancellationToken cancellationToken = default)
    {
        await EnsureCollectionExistsAsync(cancellationToken);

        if (string.IsNullOrEmpty(_collectionId))
        {
            throw new InvalidOperationException("Collection ID is not available. Collection initialization failed.");
        }

        var request = new Dictionary<string, object>
        {
            ["ids"] = new[] { chunkId }
        };

        var endpoint = $"{_settings.BaseUrl}/api/v1/collections/{_collectionId}/delete";
        var response = await _httpClient.PostAsJsonAsync(endpoint, request, JsonOptions, cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.MethodNotAllowed)
        {
            endpoint = $"{_settings.BaseUrl}/api/v1/collections/{_settings.CollectionName}/delete";
            response = await _httpClient.PostAsJsonAsync(endpoint, request, JsonOptions, cancellationToken);
        }

        response.EnsureSuccessStatusCode();
        _logger.LogInformation("Deleted chunk {ChunkId}", chunkId);
        await _bm25Service.DeleteChunkAsync(chunkId, cancellationToken);
    }

    public async Task<IReadOnlyList<Bm25RebuildChunk>> GetChunksForBm25RebuildAsync(CancellationToken cancellationToken = default)
    {
        await EnsureCollectionExistsAsync(cancellationToken);
        if (string.IsNullOrEmpty(_collectionId)) throw new InvalidOperationException("Collection ID is not available. Collection initialization failed.");

        const int pageSize = 1000;
        var chunks = new List<Bm25RebuildChunk>();
        for (var offset = 0; ; offset += pageSize)
        {
            var request = new Dictionary<string, object>
            {
                ["limit"] = pageSize,
                ["offset"] = offset,
                ["include"] = new[] { "documents", "metadatas" }
            };
            var endpoint = $"{_settings.BaseUrl}/api/v1/collections/{_collectionId}/get";
            var response = await _httpClient.PostAsJsonAsync(endpoint, request, JsonOptions, cancellationToken);
            if (response.StatusCode == System.Net.HttpStatusCode.MethodNotAllowed)
            {
                endpoint = $"{_settings.BaseUrl}/api/v1/collections/{_settings.CollectionName}/get";
                response = await _httpClient.PostAsJsonAsync(endpoint, request, JsonOptions, cancellationToken);
            }
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<ChromaGetResponse>(JsonOptions, cancellationToken);
            var ids = result?.Ids ?? [];
            var documents = result?.Documents ?? [];
            var metadatas = result?.Metadatas ?? [];
            for (var index = 0; index < ids.Count; index++)
            {
                var metadata = index < metadatas.Count ? metadatas[index] : new Dictionary<string, JsonElement>();
                var userId = GetMetadataString(metadata, "user_id");
                if (string.IsNullOrWhiteSpace(userId))
                {
                    _logger.LogWarning("Skipping Chroma chunk {ChunkId} during BM25 rebuild because user_id metadata is missing", ids[index]);
                    continue;
                }

                var documentId = GetMetadataString(metadata, "document_id");
                if (string.IsNullOrWhiteSpace(documentId))
                {
                    _logger.LogWarning("Skipping Chroma chunk {ChunkId} during BM25 rebuild because document_id metadata is missing", ids[index]);
                    continue;
                }

                chunks.Add(new Bm25RebuildChunk
                {
                    UserId = userId,
                    Chunk = new DocumentChunk
                    {
                        Id = ids[index],
                        DocumentId = documentId,
                        DocumentName = GetMetadataString(metadata, "document_name"),
                        Content = index < documents.Count ? documents[index] : string.Empty,
                        ChunkIndex = GetMetadataInt(metadata, "chunk_index"),
                        StartPosition = GetMetadataInt(metadata, "start_position"),
                        EndPosition = GetMetadataInt(metadata, "end_position"),
                        Metadata = metadata.ToDictionary(pair => pair.Key, pair => pair.Value.ToString() ?? string.Empty)
                    }
                });
            }

            if (ids.Count < pageSize) break;
        }

        _logger.LogInformation("Loaded {Count} Chroma chunks for explicit BM25 rebuild", chunks.Count);
        return chunks;
    }

    private async Task EnsureCollectionExistsAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_collectionId))
        {
            await InitializeAsync(cancellationToken);
        }
    }

    // Temporary diagnostic logging for verifying the active collection's distance metric.
    private async Task LogCollectionDiagnosticsAsync(ChromaCollection? collection, CancellationToken cancellationToken)
    {
        string? serverVersion = null;

        try
        {
            var versionResponse = await _httpClient.GetAsync(
                $"{_settings.BaseUrl}/api/v1/version",
                cancellationToken);

            if (versionResponse.IsSuccessStatusCode)
            {
                serverVersion = await versionResponse.Content.ReadFromJsonAsync<string>(JsonOptions, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Unable to retrieve Chroma server version for collection diagnostics");
        }

        var metadata = collection?.Metadata?.GetRawText();
        var configuration = collection?.Configuration?.GetRawText();
        var hnswSpace = GetHnswSpace(collection?.Metadata, collection?.Configuration);

        _logger.LogInformation(
            "Chroma collection diagnostics. CollectionId={CollectionId}, CollectionName={CollectionName}, Metadata={Metadata}, Configuration={Configuration}, HnswSpace={HnswSpace}, ServerVersion={ServerVersion}",
            collection?.Id,
            collection?.Name,
            metadata,
            configuration,
            hnswSpace,
            serverVersion);
    }

    private static string? GetHnswSpace(JsonElement? metadata, JsonElement? configuration)
    {
        if (metadata is { ValueKind: JsonValueKind.Object } metadataObject &&
            metadataObject.TryGetProperty("hnsw:space", out var metadataSpace))
        {
            return metadataSpace.GetString();
        }

        if (configuration is { ValueKind: JsonValueKind.Object } configurationObject &&
            configurationObject.TryGetProperty("hnsw", out var hnsw) &&
            hnsw.ValueKind == JsonValueKind.Object &&
            hnsw.TryGetProperty("space", out var configurationSpace))
        {
            return configurationSpace.GetString();
        }

        return null;
    }

    private static string GetMetadataString(Dictionary<string, JsonElement> metadata, string key)
    {
        if (metadata.TryGetValue(key, out var value))
        {
            return value.ValueKind == JsonValueKind.String ? value.GetString() ?? string.Empty : value.ToString();
        }
        return string.Empty;
    }

    private static int GetMetadataInt(Dictionary<string, JsonElement> metadata, string key)
    {
        if (!metadata.TryGetValue(key, out var value)) return 0;
        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number)) return number;
        return int.TryParse(value.ToString(), out number) ? number : 0;
    }

    private sealed class ChromaCollection
    {
        [JsonPropertyName("id")]
        public string? Id { get; init; }

        [JsonPropertyName("name")]
        public string? Name { get; init; }

        [JsonPropertyName("metadata")]
        public JsonElement? Metadata { get; init; }

        [JsonPropertyName("configuration")]
        public JsonElement? Configuration { get; init; }
    }

    private sealed class ChromaQueryResponse
    {
        [JsonPropertyName("ids")]
        public List<List<string>>? Ids { get; init; }

        [JsonPropertyName("documents")]
        public List<List<string>>? Documents { get; init; }

        [JsonPropertyName("metadatas")]
        public List<List<Dictionary<string, JsonElement>>>? Metadatas { get; init; }

        [JsonPropertyName("distances")]
        public List<List<float>>? Distances { get; init; }
    }

    private sealed class ChromaGetResponse
    {
        [JsonPropertyName("ids")]
        public List<string>? Ids { get; init; }

        [JsonPropertyName("documents")]
        public List<string>? Documents { get; init; }

        [JsonPropertyName("metadatas")]
        public List<Dictionary<string, JsonElement>>? Metadatas { get; init; }

        [JsonPropertyName("embeddings")]
        public List<List<float>>? Embeddings { get; init; }
    }
}

