using DocumentAI.DTOs.Requests;
using DocumentAI.DTOs.Responses;
using DocumentAI.Configuration;
using DocumentAI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace DocumentAI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class DocumentsController : ControllerBase
{
    private readonly IDocumentParserService _documentParser;
    private readonly IChunkingService _chunkingService;
    private readonly IEmbeddingService _embeddingService;
    private readonly IChromaService _chromaService;
    private readonly IHybridSearchService _hybridSearchService;
    private readonly ILlmService _llmService;
    private readonly ChunkingSettings _chunkingSettings;
    private readonly ILogger<DocumentsController> _logger;
    private readonly IQueryLogService _queryLogService;

    public DocumentsController(
        IDocumentParserService documentParser,
        IChunkingService chunkingService,
        IEmbeddingService embeddingService,
        IChromaService chromaService,
        IHybridSearchService hybridSearchService,
        ILlmService llmService,
        IOptions<ChunkingSettings> chunkingSettings,
        ILogger<DocumentsController> logger,
        IQueryLogService queryLogService)
    {
        _documentParser = documentParser;
        _chunkingService = chunkingService;
        _embeddingService = embeddingService;
        _chromaService = chromaService;
        _hybridSearchService = hybridSearchService;
        _llmService = llmService;
        _chunkingSettings = chunkingSettings.Value;
        _logger = logger;
        _queryLogService = queryLogService;
    }

    /// <summary>
    /// Upload and process a document for RAG
    /// </summary>
    /// <param name="file">The document file (PDF, DOCX, or TXT)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Upload result with document details</returns>
    [Authorize]
    [HttpPost("upload")]
    [RequestSizeLimit(50 * 1024 * 1024)] // 50MB limit
    [ProducesResponseType(typeof(UploadDocumentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Upload(
        IFormFile file,
        [FromForm] int? chunkSize,
        [FromForm] int? chunkOverlap,
        CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(CreateErrorResponse("Invalid file", "No file was uploaded or file is empty"));
        }

        if (!_documentParser.IsSupportedFileType(file.FileName))
        {
            var supported = string.Join(", ", _documentParser.GetSupportedExtensions());
            return BadRequest(CreateErrorResponse(
                "Unsupported file type",
                $"The file type is not supported. Supported types: {supported}"));
        }

        if (chunkSize is < 128 or > 10000)
        {
            return BadRequest(CreateErrorResponse("Invalid chunk size", "ChunkSize must be between 128 and 10000"));
        }

        var effectiveChunkSize = chunkSize ?? _chunkingSettings.ChunkSize;
        if (chunkOverlap is < 0 or > 5000 || (chunkOverlap.HasValue && chunkOverlap >= effectiveChunkSize))
        {
            return BadRequest(CreateErrorResponse("Invalid chunk overlap", "ChunkOverlap must be less than ChunkSize and between 0 and 5000"));
        }

        var stopwatch = Stopwatch.StartNew();
        var documentId = Guid.NewGuid().ToString("N");

        _logger.LogInformation("Processing document upload: {FileName}, Size: {Size} bytes, DocumentId: {DocumentId}",
            file.FileName, file.Length, documentId);
        string? userId = HttpContext.User.FindFirst("UserId")?.Value;
        try
        {
            // 1. Extract text from document
            await using var stream = file.OpenReadStream();
            var text = await _documentParser.ExtractTextAsync(stream, file.FileName, cancellationToken);

            if (string.IsNullOrWhiteSpace(text))
            {
                return BadRequest(CreateErrorResponse(
                    "Empty document",
                    "No text could be extracted from the document"));
            }

            // 2. Chunk the document
            var chunks = _chunkingService.ChunkDocument(documentId, file.FileName, text, chunkSize, chunkOverlap);

            if (chunks.Count == 0)
            {
                return BadRequest(CreateErrorResponse(
                    "Chunking failed",
                    "Document could not be split into chunks"));
            }

            // 3. Generate embeddings for all chunks
            var chunkTexts = chunks.Select(c => c.Content).ToList();
            var embeddings = await _embeddingService.GenerateEmbeddingsAsync(chunkTexts, cancellationToken);

            // 4. Store in Chroma
            await _chromaService.StoreChunksAsync(userId,chunks, embeddings, cancellationToken);

            stopwatch.Stop();

            _logger.LogInformation(
                "Document processed successfully: {FileName}, Chunks: {ChunkCount}, Time: {ElapsedMs}ms",
                file.FileName, chunks.Count, stopwatch.ElapsedMilliseconds);

            return Ok(new UploadDocumentResponse
            {
                DocumentId = documentId,
                FileName = file.FileName,
                ChunkCount = chunks.Count,
                CharacterCount = text.Length,
                ProcessedAt = DateTime.UtcNow,
                Message = $"Document processed successfully in {stopwatch.ElapsedMilliseconds}ms"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process document: {FileName}", file.FileName);
            return StatusCode(500, CreateErrorResponse(
                "Processing failed",
                "An error occurred while processing the document",
                ex.Message));
        }
    }

    /// <summary>
    /// Query documents using RAG
    /// </summary>
    /// <param name="request">The query request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>AI-generated answer grounded in document context</returns>
    [HttpPost("query")]
    [ProducesResponseType(typeof(QueryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Query([FromBody] QueryRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(CreateErrorResponse("Validation failed", ModelState.ToString() ?? "Invalid request"));
        }

        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation("Processing query: {Query}, TopK: {TopK}", request.Query, request.TopK);

        try
        {
            string? userId = HttpContext.User.FindFirst("UserId")?.Value;
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized();
            }

            // 1. Retrieve relevant chunks using the configured Vector, BM25, or Hybrid strategy.
            var contexts = await _hybridSearchService.HybridSearchAsync(userId,
                request.Query,
                request.TopK,
                request.DocumentId,
                request.DocumentIds,
                cancellationToken);

            // 2. Generate answer using LLM
            var (answer, foundInContext) = await _llmService.AnswerQueryAsync(request.ModelId,
                request.Query,
                contexts,
                cancellationToken);

            stopwatch.Stop();

            // 3. Build response with sources
            var sources = contexts.Select(ctx => new SourceReference
            {
                DocumentId = ctx.DocumentId,
                DocumentName = ctx.DocumentName,
                ChunkId = ctx.ChunkId,
                Excerpt = TruncateExcerpt(ctx.Content, 200),
                RelevanceScore = ctx.Similarity ?? ctx.Distance ?? 0f
            }).ToList();

            _logger.LogInformation("Query processed in {ElapsedMs}ms, Sources: {SourceCount}",
                stopwatch.ElapsedMilliseconds, sources.Count);

            if (!string.IsNullOrWhiteSpace(userId))
            {
                await _queryLogService.CreateAsync(
                    userId,
                    request.Query,
                    answer,
                    sources.Count > 0 ? sources.Max(source => source.RelevanceScore) : null,
                    stopwatch.Elapsed.TotalMilliseconds,
                    cancellationToken);
            }

            return Ok(new QueryResponse
            {
                Answer = answer,
                Sources = sources,
                ProcessingTimeMs = stopwatch.ElapsedMilliseconds,
                AnswerFoundInContext = foundInContext
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process query: {Query}", request.Query);
            return StatusCode(500, CreateErrorResponse(
                "Query failed",
                "An error occurred while processing the query",
                ex.Message));
        }
    }

    /// <summary>
    /// Get all query logs, or logs for a specific user when userId is supplied.
    /// </summary>
    [Authorize]
    [HttpGet("query-logs")]
    [ProducesResponseType(typeof(List<QueryLogResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetQueryLogs([FromQuery] string? userId, CancellationToken cancellationToken)
    {
        var logs = await _queryLogService.GetAsync(userId, cancellationToken);

        return Ok(logs);
    }

    /// <summary>
    /// Generate a structured business report from documents
    /// </summary>
    /// <param name="request">The report request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Structured business report</returns>
    [HttpPost("report")]
    [ProducesResponseType(typeof(ReportResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GenerateReport([FromBody] ReportRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(CreateErrorResponse("Validation failed", ModelState.ToString() ?? "Invalid request"));
        }
        string? userId = HttpContext.User.FindFirst("UserId")?.Value;
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation("Generating report for topic: {Topic}, TopK: {TopK}", request.Topic, request.TopK);

        try
        {
            // 1. Generate embedding for topic
            var topicEmbedding = await _embeddingService.GenerateEmbeddingAsync(request.Topic, cancellationToken);

            // 2. Retrieve relevant chunks from Chroma
            var contexts = await _chromaService.QueryAsync(userId,
                topicEmbedding,
                request.TopK,
                request.DocumentId,
                null,
                cancellationToken);

            // 3. Generate report using LLM
            var report = await _llmService.GenerateReportAsync(request.ModelId,
                request.Topic,
                contexts,
                request.AdditionalInstructions,
                cancellationToken);

            stopwatch.Stop();

            // 4. Build response with sources
            var sources = contexts.Select(ctx => new SourceReference
            {
                DocumentId = ctx.DocumentId,
                DocumentName = ctx.DocumentName,
                ChunkId = ctx.ChunkId,
                Excerpt = TruncateExcerpt(ctx.Content, 200),
                RelevanceScore = ctx.Similarity ?? ctx.Distance ?? 0f
            }).ToList();

            _logger.LogInformation("Report generated in {ElapsedMs}ms, Sources: {SourceCount}",
                stopwatch.ElapsedMilliseconds, sources.Count);

            return Ok(new ReportResponse
            {
                Topic = request.Topic,
                Report = report,
                Sources = sources,
                GeneratedAt = DateTime.UtcNow,
                ProcessingTimeMs = stopwatch.ElapsedMilliseconds
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate report for topic: {Topic}", request.Topic);
            return StatusCode(500, CreateErrorResponse(
                "Report generation failed",
                "An error occurred while generating the report",
                ex.Message));
        }
    }

    /// <summary>
    /// Health check endpoint
    /// </summary>
    [HttpGet("health")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> HealthCheck(CancellationToken cancellationToken)
    {
        var chromaHealthy = await _chromaService.HealthCheckAsync(cancellationToken);
        var llmHealthy = await _llmService.HealthCheckAsync(cancellationToken);

        var status = new
        {
            Status = chromaHealthy && llmHealthy ? "Healthy" : "Degraded",
            Timestamp = DateTime.UtcNow,
            Services = new
            {
                ChromaDb = chromaHealthy ? "Healthy" : "Unhealthy",
                Ollama = llmHealthy ? "Healthy" : "Unhealthy"
            }
        };

        return Ok(status);
           
    }

    [HttpGet("models")]
    public async Task<IActionResult> GetModels(CancellationToken cancellationToken)
    {
        var result = await _llmService.GetModelsAsync(cancellationToken);

        return Content(result, "application/json");
    }

    private static string TruncateExcerpt(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
            return text;

        return text[..(maxLength - 3)] + "...";
    }

    private ErrorResponse CreateErrorResponse(string error, string message, string? details = null)
    {
        return new ErrorResponse
        {
            Error = error,
            Message = message,
            Details = details,
            Timestamp = DateTime.UtcNow,
            TraceId = HttpContext.TraceIdentifier
        };
    }
}

