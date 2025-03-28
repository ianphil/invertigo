using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using VectorSearch.Api.Models;
using VectorSearch.Core;

namespace VectorSearch.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SearchController : ControllerBase
{
    private readonly IEmbeddingService _embeddingService;
    private readonly IVFSearchEngine _searchEngine;
    private readonly ILogger<SearchController> _logger;

    public SearchController(
        IEmbeddingService embeddingService, 
        IVFSearchEngine searchEngine,
        ILogger<SearchController> logger)
    {
        _embeddingService = embeddingService ?? throw new ArgumentNullException(nameof(embeddingService));
        _searchEngine = searchEngine ?? throw new ArgumentNullException(nameof(searchEngine));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Search for vectors similar to the query
    /// </summary>
    /// <param name="request">The search request parameters</param>
    /// <returns>A list of search results</returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<IEnumerable<Models.SearchResult>>> Search(SearchRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Query))
            {
                return BadRequest("Query cannot be empty");
            }

            _logger.LogInformation("Processing search request for query: {Query}", request.Query);

            // Get embedding for the query using the embedding service
            var queryEmbedding = await _embeddingService.GetEmbeddingAsync(request.Query);

            // Search using the IVFSearchEngine
            var searchResults = _searchEngine.Search(
                queryEmbedding,
                request.TopK,
                request.NProbes,
                request.TagFilter,
                request.CosineWeight,
                request.MetadataWeight);

            // Map core search results to API model
            var results = searchResults.Select(result => new Models.SearchResult
            {
                Id = result.Item.Id,
                Metadata = result.Item.Metadata,
                CosineScore = result.CosineScore,
                MetadataScore = result.MetadataScore,
                HybridScore = result.HybridScore
            }).ToList();

            return Ok(results);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid search request");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing search request");
            return StatusCode(StatusCodes.Status502BadGateway, "Error processing request: " + ex.Message);
        }
    }
}