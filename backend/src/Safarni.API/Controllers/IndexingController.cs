using System;
using System.Diagnostics;
using System.Threading.Tasks;
using ChatBotApi.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ChatBotApi.Controllers;

[ApiController]
[Route("api/indexing")]
public class IndexingController : ControllerBase
{
    private readonly IIndexingService _indexingService;
    private readonly IQdrantService _qdrantService;
    private readonly ILogger<IndexingController> _logger;

    public IndexingController(
        IIndexingService indexingService, 
        IQdrantService qdrantService,
        ILogger<IndexingController> logger)
    {
        _indexingService = indexingService;
        _qdrantService = qdrantService;
        _logger = logger;
    }

    [HttpPost("index")]
    public async Task<IActionResult> IndexAll()
    {
        _logger.LogInformation("HTTP POST api/indexing/index requested.");
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var count = await _indexingService.IndexAllAsync();
            stopwatch.Stop();

            return Ok(new
            {
                success = true,
                message = $"Successfully indexed travel documents. Created and saved {count} vector chunks.",
                processedChunksCount = count,
                executionTimeMs = stopwatch.ElapsedMilliseconds
            });
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "HTTP POST api/indexing/index failed.");
            return StatusCode(500, new
            {
                success = false,
                message = $"Indexing failed: {ex.Message}",
                executionTimeMs = stopwatch.ElapsedMilliseconds
            });
        }
    }

    [HttpPost("reindex")]
    public async Task<IActionResult> ReindexAll()
    {
        _logger.LogInformation("HTTP POST api/indexing/reindex requested.");
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var count = await _indexingService.ReindexAllAsync();
            stopwatch.Stop();

            return Ok(new
            {
                success = true,
                message = $"Successfully re-indexed travel documents. Recreated Qdrant collection and saved {count} vector chunks.",
                processedChunksCount = count,
                executionTimeMs = stopwatch.ElapsedMilliseconds
            });
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "HTTP POST api/indexing/reindex failed.");
            return StatusCode(500, new
            {
                success = false,
                message = $"Reindexing failed: {ex.Message}",
                executionTimeMs = stopwatch.ElapsedMilliseconds
            });
        }
    }

    [HttpGet("health")]
    public async Task<IActionResult> GetHealth()
    {
        _logger.LogInformation("HTTP GET api/indexing/health requested.");
        
        bool qdrantHealthy = await _qdrantService.IsHealthyAsync();

        return Ok(new
        {
            status = qdrantHealthy ? "Healthy" : "Degraded",
            qdrantAvailable = qdrantHealthy
        });
    }
}
