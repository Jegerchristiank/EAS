using EsgAsAService.Api.Models;
using EsgAsAService.Domain.Entities.Core;
using EsgAsAService.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EsgAsAService.Api.Controllers;

[ApiController]
[Route("governance/cases")] 
public class GovernanceCasesController(EsgDbContext db) : ControllerBase
{
    private readonly EsgDbContext _db = db;

    [HttpGet]
    [Authorize(Policy = "CanRead")]
    [ProducesResponseType(typeof(PagedResponse<GovernanceCaseResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<GovernanceCaseResponse>>> List([FromQuery] Guid periodId, [FromQuery] int page = 1, [FromQuery] int pageSize = 100)
    {
        pageSize = Math.Clamp(pageSize, 1, 500);
        var query = _db.GovernanceCases.AsNoTracking().Where(x => x.ReportingPeriodId == periodId);
        var total = await query.CountAsync();
        var items = await query.OrderBy(x => x.Type)
            .ThenByDescending(x => x.Amount)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new GovernanceCaseResponse(x.Id, x.OrganisationId, x.ReportingPeriodId, x.Type, x.Outcome, x.Amount, x.CaseRef))
            .ToListAsync();
        return Ok(new PagedResponse<GovernanceCaseResponse>(total, page, pageSize, items));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "CanRead")]
    [ProducesResponseType(typeof(GovernanceCaseResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GovernanceCaseResponse>> Get(Guid id)
    {
        var item = await _db.GovernanceCases.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new GovernanceCaseResponse(x.Id, x.OrganisationId, x.ReportingPeriodId, x.Type, x.Outcome, x.Amount, x.CaseRef))
            .FirstOrDefaultAsync();
        if (item is null) return NotFound();
        return Ok(item);
    }

    [HttpPost]
    [Authorize(Policy = "CanIngestData")]
    [ProducesResponseType(typeof(IdResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IdResponse>> Create([FromBody] GovernanceCaseRequest req)
    {
        if (req is null) return BadRequest();
        var g = new GovernanceCase
        {
            OrganisationId = req.OrganisationId,
            ReportingPeriodId = req.ReportingPeriodId,
            Type = req.Type,
            Outcome = req.Outcome,
            Amount = req.Amount,
            CaseRef = req.CaseRef
        };
        _db.Add(g);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = g.Id }, new IdResponse(g.Id));
    }
}
