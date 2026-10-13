using DocumentAI.DTOs.Responses;
using DocumentAI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DocumentAI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public sealed class EmbeddingsController : ControllerBase
{
    private readonly IChromaService _chromaService;
    private readonly IBm25Service _bm25Service;
    private readonly ILogger<EmbeddingsController> _logger;

    public EmbeddingsController(
        IChromaService chromaService,
        IBm25Service bm25Service,
        ILogger<EmbeddingsController> logger)
    {
        _chromaService = chromaService;
        _bm25Service = bm25Service;
        _logger = logger;
    }

    /// <summary>
    /// Get all distinct embedded documents
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of embedded documents with metadata</returns>
    [Authorize]
    [HttpGet("documents")]
    [ProducesResponseType(typeof(List<EmbeddedDocumentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetDocuments(CancellationToken cancellationToken)
    {
        try
        {
            string? userId = HttpContext.User.FindFirst("UserId")?.Value;
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(CreateErrorResponse(
                    "Unauthorized",
                    "User ID is missing in the token"));
            }
            _logger.LogInformation("Retrieving all embedded documents");
            var documents = await _chromaService.GetEmbeddedDocumentsAsync(userId, cancellationToken);
            return Ok(documents);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to retrieve embedded documents - HTTP error");
            return StatusCode(422, CreateErrorResponse(
                "Unprocessable Entity",
                "Failed to retrieve embedded documents from Chroma DB",
                ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve embedded documents");
            return StatusCode(500, CreateErrorResponse(
                "Retrieval failed",
                "An error occurred while retrieving embedded documents",
                ex.Message));
        }
    }

    /// <summary>
    /// Get all chunks for a specific document
    /// </summary>
    /// <param name="documentId">The document ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of chunks for the document</returns>
    [HttpGet("documents/{documentId}/chunks")]
    [ProducesResponseType(typeof(List<EmbeddedChunkResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetDocumentChunks(string userId,
        [FromRoute] string documentId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(documentId))
        {
            return BadRequest(CreateErrorResponse(
                "Invalid request",
                "Document ID is required"));
        }

        try
        {
            _logger.LogInformation("Retrieving chunks for document {DocumentId}", documentId);
            var chunks = await _chromaService.GetDocumentChunksAsync(userId,documentId, cancellationToken);
            return Ok(chunks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve chunks for document {DocumentId}", documentId);
            return StatusCode(500, CreateErrorResponse(
                "Retrieval failed",
                "An error occurred while retrieving document chunks",
                ex.Message));
        }
    }

    /// <summary>
    /// Delete all embeddings for a specific document
    /// </summary>
    /// <param name="documentId">The document ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success response</returns>
    [HttpDelete("documents/{documentId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteDocument(
        [FromRoute] string documentId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(documentId))
        {
            return BadRequest(CreateErrorResponse(
                "Invalid request",
                "Document ID is required"));
        }

        try
        {
            _logger.LogInformation("Deleting all embeddings for document {DocumentId}", documentId);
            await _chromaService.DeleteDocumentAsync(documentId, cancellationToken);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete embeddings for document {DocumentId}", documentId);
            return StatusCode(500, CreateErrorResponse(
                "Deletion failed",
                "An error occurred while deleting document embeddings",
                ex.Message));
        }
    }

    /// <summary>
    /// Delete a specific embedding chunk
    /// </summary>
    /// <param name="chunkId">The chunk ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success response</returns>
    [HttpDelete("chunks/{chunkId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteChunk(
        [FromRoute] string chunkId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(chunkId))
        {
            return BadRequest(CreateErrorResponse(
                "Invalid request",
                "Chunk ID is required"));
        }

        try
        {
            _logger.LogInformation("Deleting chunk {ChunkId}", chunkId);
            await _chromaService.DeleteChunkAsync(chunkId, cancellationToken);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete chunk {ChunkId}", chunkId);
            return StatusCode(500, CreateErrorResponse(
                "Deletion failed",
                "An error occurred while deleting the chunk",
                ex.Message));
        }
    }

    /// <summary>
    /// Explicitly rebuild the persistent BM25 index from existing Chroma chunks.
    /// </summary>
    [Authorize]
    [HttpPost("rebuild-bm25-index")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> RebuildBm25Index(CancellationToken cancellationToken)
    {
        try
        {
            var chunks = await _chromaService.GetChunksForBm25RebuildAsync(cancellationToken);
            await _bm25Service.RebuildAsync(chunks, cancellationToken);
            return Ok(new { IndexedChunkCount = chunks.Count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to rebuild BM25 index from Chroma chunks");
            return StatusCode(500, CreateErrorResponse("BM25 rebuild failed", "An error occurred while rebuilding the BM25 index", ex.Message));
        }
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

