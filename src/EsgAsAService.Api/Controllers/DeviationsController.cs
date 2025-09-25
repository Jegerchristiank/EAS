using EsgAsAService.Api.Models;
using EsgAsAService.Domain.Entities.Core;
using EsgAsAService.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EsgAsAService.Api.Controllers;

[ApiController]
[Route("deviations")]
public class DeviationsController : ControllerBase
{
    private readonly EsgDbContext _db;
    public DeviationsController(EsgDbContext db) => _db = db;

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "CanRead")]
    [ProducesResponseType(typeof(DeviationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DeviationResponse>> Get(Guid id)
    {
        var dev = await _db.Deviations.AsNoTracking()
            .Where(d => d.Id == id)
            .Select(d => new DeviationResponse(d.Id, d.ScopeEntryId, d.Reason))
            .FirstOrDefaultAsync();
        if (dev is null) return NotFound();
        return Ok(dev);
    }

    [HttpPost]
    [Authorize(Policy = "CanIngestData")]
    [ProducesResponseType(typeof(IdResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IdResponse>> Create([FromBody] DeviationRequest req)
    {
        if (req is null) return BadRequest();
        var dev = new Deviation { ScopeEntryId = req.ScopeEntryId, Reason = req.Reason };
        _db.Add(dev);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = dev.Id }, new IdResponse(dev.Id));
    }
}
