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
    private readonly ILogger<EmbeddingController> _logger;

    /// <summary>
    /// Initializes a new instance of the EmbeddingController
    /// </summary>
    /// <param name="embeddingService">The service used to generate embeddings</param>
    /// <param name="logger">The logger</param>
    public EmbeddingController(
        IEmbeddingService embeddingService,
        ILogger<EmbeddingController> logger)
    {
        _embeddingService = embeddingService ?? throw new ArgumentNullException(nameof(embeddingService));
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
                // Token count information isn't available from the current embedding service
                // but the model is designed to support it if added in the future
            };

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