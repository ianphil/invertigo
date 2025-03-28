using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using VectorSearch.Api.Models;
using VectorSearch.Core;

namespace VectorSearch.Api.Controllers;

/// <summary>
/// Controller for generating embeddings from text
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class EmbeddingController : ControllerBase
{
    private readonly IEmbeddingService _embeddingService;
    private readonly IndexManager _indexManager;
    private readonly ILogger<EmbeddingController> _logger;

    /// <summary>
    /// Initializes a new instance of the EmbeddingController
    /// </summary>
    /// <param name="embeddingService">The service used to generate embeddings</param>
    /// <param name="indexManager">The manager for the vector index</param>
    /// <param name="logger">The logger</param>
    public EmbeddingController(
        IEmbeddingService embeddingService,
        IndexManager indexManager,
        ILogger<EmbeddingController> logger)
    {
        _embeddingService = embeddingService ?? throw new ArgumentNullException(nameof(embeddingService));
        _indexManager = indexManager ?? throw new ArgumentNullException(nameof(indexManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Generates an embedding vector for the provided text
    /// </summary>
    /// <param name="request">The embedding request containing text to embed</param>
    /// <returns>The embedding vector</returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<EmbeddingResponse>> GenerateEmbedding(EmbeddingRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Text))
            {
                return BadRequest("Text to embed cannot be empty");
            }

            _logger.LogInformation("Processing embedding request for text (length: {Length})", request.Text.Length);

            // Get embedding using the embedding service
            var embedding = await _embeddingService.GetEmbeddingAsync(request.Text);

            // Create and return the response
            var response = new EmbeddingResponse
            {
                Embedding = embedding,
                Persisted = false // Default to false until we persist successfully
            };

            // Persist the embedding if requested
            if (request.Persist)
            {
                // Generate an ID if not provided
                string id = request.Id ?? Guid.NewGuid().ToString();

                // Create metadata if not provided
                var metadata = request.Metadata ?? new Dictionary<string, string>();
                
                // Add the original text to metadata if not already present
                if (!metadata.ContainsKey("text"))
                {
                    // Truncate text if it's too long (over 500 chars)
                    string textForMetadata = request.Text;
                    if (textForMetadata.Length > 500)
                    {
                        textForMetadata = textForMetadata.Substring(0, 497) + "...";
                    }
                    metadata["text"] = textForMetadata;
                }

                // Create the vector item
                var vectorItem = new VectorItem
                {
                    Id = id,
                    Vector = embedding,
                    Metadata = metadata,
                    PrecomputedScore = request.Score
                };

                // Add to index and save
                bool success = await _indexManager.AddItemAsync(vectorItem);
                
                if (success)
                {
                    response.Persisted = true;
                    response.Id = id;
                    _logger.LogInformation("Successfully persisted embedding with ID {Id}", id);
                }
                else
                {
                    _logger.LogWarning("Failed to persist embedding due to dimension mismatch");
                }
            }

            _logger.LogInformation("Generated embedding with {Dimensions} dimensions", response.Dimensions);
            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid embedding request");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating embedding");
            return StatusCode(StatusCodes.Status502BadGateway, "Error generating embedding: " + ex.Message);
        }
    }
}