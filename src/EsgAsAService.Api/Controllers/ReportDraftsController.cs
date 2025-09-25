using EsgAsAService.Api.Models;
using EsgAsAService.Domain.Entities.Core;
using EsgAsAService.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EsgAsAService.Api.Controllers;

[ApiController]
[Route("report-drafts")]
public class ReportDraftsController : ControllerBase
{
    private readonly EsgDbContext _db;
    public ReportDraftsController(EsgDbContext db) => _db = db;

    [HttpGet]
    [Authorize(Policy = "CanRead")]
    [ProducesResponseType(typeof(IReadOnlyList<ReportDraftResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ReportDraftResponse>>> List([FromQuery] Guid periodId)
    {
        var drafts = await _db.ReportDrafts.AsNoTracking()
            .Where(r => r.ReportingPeriodId == periodId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new ReportDraftResponse(r.Id, r.ReportingPeriodId, r.Json, r.CreatedAt))
            .ToListAsync();
        return Ok(drafts);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "CanRead")]
    [ProducesResponseType(typeof(ReportDraftResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReportDraftResponse>> Get(Guid id)
    {
        var draft = await _db.ReportDrafts.AsNoTracking()
            .Where(r => r.Id == id)
            .Select(r => new ReportDraftResponse(r.Id, r.ReportingPeriodId, r.Json, r.CreatedAt))
            .FirstOrDefaultAsync();
        if (draft is null) return NotFound();
        return Ok(draft);
    }

    [HttpPost]
    [Authorize(Policy = "CanCalculate")]
    [ProducesResponseType(typeof(IdResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IdResponse>> Create([FromBody] ReportDraft req)
    {
        if (req is null) return BadRequest();
        var rd = new ReportDraft { ReportingPeriodId = req.ReportingPeriodId, Json = req.Json };
        _db.Add(rd);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = rd.Id }, new IdResponse(rd.Id));
    }
}
