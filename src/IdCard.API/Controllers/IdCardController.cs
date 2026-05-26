using IdCard.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace IdCard.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public sealed class IdCardController : ControllerBase
{
    private readonly IdCardAggregator _aggregator;
    private readonly ILogger<IdCardController> _logger;

    public IdCardController(IdCardAggregator aggregator, ILogger<IdCardController> logger)
    {
        _aggregator = aggregator;
        _logger     = logger;
    }

    /// <summary>
    /// Returns front and back ID card images as Base64-encoded PNG strings.
    /// </summary>
    /// <param name="memberId">Member identifier (e.g. MED-001, DEN-001, VIS-001)</param>
    /// <param name="lob">Line of business (e.g. MEDICAL, DENTAL, VISION)</param>
    [HttpGet("{memberId}/{lob}")]
    [ProducesResponseType(typeof(IdCardResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetIdCard(
        string memberId, string lob, CancellationToken ct)
    {
        try
        {
            var result = await _aggregator.GenerateAsync(memberId, lob, ct);
            return Ok(new IdCardResponse
            {
                MemberId       = memberId,
                Lob            = lob.ToUpperInvariant(),
                FrontImageB64  = Convert.ToBase64String(result.FrontImageBytes),
                BackImageB64   = Convert.ToBase64String(result.BackImageBytes),
                FrontMimeType  = "image/png",
                BackMimeType   = "image/png"
            });
        }
        catch (FileNotFoundException ex)
        {
            _logger.LogWarning(ex, "Template not found for Member={MemberId} LOB={Lob}", memberId, lob);
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating ID card for Member={MemberId} LOB={Lob}", memberId, lob);
            return StatusCode(500, new { error = "ID card generation failed.", detail = ex.Message });
        }
    }

    /// <summary>Returns the front image as a raw PNG file download.</summary>
    [HttpGet("{memberId}/{lob}/front")]
    [Produces("image/png")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFront(string memberId, string lob, CancellationToken ct)
    {
        try
        {
            var result = await _aggregator.GenerateAsync(memberId, lob, ct);
            return File(result.FrontImageBytes, "image/png", $"{memberId}_{lob}_front.png");
        }
        catch (FileNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>Returns the back image as a raw PNG file download.</summary>
    [HttpGet("{memberId}/{lob}/back")]
    [Produces("image/png")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBack(string memberId, string lob, CancellationToken ct)
    {
        try
        {
            var result = await _aggregator.GenerateAsync(memberId, lob, ct);
            return File(result.BackImageBytes, "image/png", $"{memberId}_{lob}_back.png");
        }
        catch (FileNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }
}

public sealed record IdCardResponse
{
    public string MemberId      { get; init; } = string.Empty;
    public string Lob           { get; init; } = string.Empty;
    public string FrontImageB64 { get; init; } = string.Empty;
    public string BackImageB64  { get; init; } = string.Empty;
    public string FrontMimeType { get; init; } = "image/png";
    public string BackMimeType  { get; init; } = "image/png";
}
