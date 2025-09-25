using EsgAsAService.Api.Auth;
using EsgAsAService.Api.Models;
using EsgAsAService.Domain.Entities.Core;
using EsgAsAService.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EsgAsAService.Api.Controllers;

[ApiController]
[Route("scope-entries")]
public class ScopeEntriesController : ControllerBase
{
    private readonly EsgDbContext _db;
    public ScopeEntriesController(EsgDbContext db) => _db = db;

    [HttpGet]
    [Authorize(Policy = "CanRead")]
    [ProducesResponseType(typeof(PagedResponse<ScopeEntryResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<ScopeEntryResponse>>> List([FromQuery] Guid activityId, [FromQuery] int page = 1, [FromQuery] int pageSize = 100)
    {
        pageSize = Math.Clamp(pageSize, 1, 500);
        var query = _db.ScopeEntries.AsNoTracking().Where(s => activityId == Guid.Empty || s.ActivityId == activityId);
        var total = await query.CountAsync();
        var items = await query.OrderBy(s => s.ActivityId).ThenBy(s => s.Scope)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new ScopeEntryResponse(s.Id, s.ActivityId, s.Scope, s.EmissionFactorId, s.Adjustment))
            .ToListAsync();
        return Ok(new PagedResponse<ScopeEntryResponse>(total, page, pageSize, items));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "CanRead")]
    [ProducesResponseType(typeof(ScopeEntryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ScopeEntryResponse>> Get(Guid id)
    {
        var entry = await _db.ScopeEntries.AsNoTracking()
            .Where(s => s.Id == id)
            .Select(s => new ScopeEntryResponse(s.Id, s.ActivityId, s.Scope, s.EmissionFactorId, s.Adjustment))
            .FirstOrDefaultAsync();
        if (entry is null) return NotFound();
        return Ok(entry);
    }

    [HttpPost]
    [Authorize(Policy = "CanIngestData")]
    [ProducesResponseType(typeof(IdResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IdResponse>> Create([FromBody] ScopeEntryRequest req)
    {
        if (req is null) return BadRequest();
        var s = new ScopeEntry { ActivityId = req.ActivityId, Scope = req.Scope, EmissionFactorId = req.EmissionFactorId, Adjustment = req.Adjustment };
        _db.Add(s);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = s.Id }, new IdResponse(s.Id));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "CanIngestData")]
    public async Task<IActionResult> Update(Guid id, [FromBody] ScopeEntryRequest req)
    {
        if (req is null) return BadRequest();
        var s = await _db.ScopeEntries.FindAsync(id);
        if (s == null) return NotFound();
        s.ActivityId = req.ActivityId; s.Scope = req.Scope; s.EmissionFactorId = req.EmissionFactorId; s.Adjustment = req.Adjustment;
        _db.Update(s); await _db.SaveChangesAsync();
        return Ok(new IdResponse(s.Id));
    }
}
