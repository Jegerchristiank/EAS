using EsgAsAService.Api.Models;
using EsgAsAService.Domain.Entities.Core;
using EsgAsAService.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EsgAsAService.Api.Controllers;

[ApiController]
[Route("pollution")] 
public class PollutionController(EsgDbContext db) : ControllerBase
{
    private readonly EsgDbContext _db = db;

    [HttpGet]
    [Authorize(Policy = "CanRead")]
    [ProducesResponseType(typeof(PagedResponse<PollutionRegisterResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<PollutionRegisterResponse>>> List([FromQuery] Guid periodId, [FromQuery] int page = 1, [FromQuery] int pageSize = 100)
    {
        pageSize = Math.Clamp(pageSize, 1, 500);
        var query = _db.PollutionRegisters.AsNoTracking().Where(x => x.ReportingPeriodId == periodId);
        var total = await query.CountAsync();
        var items = await query.OrderBy(x => x.Substance)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new PollutionRegisterResponse(x.Id, x.OrganisationId, x.ReportingPeriodId, x.LocationId, x.Substance, x.Quantity, x.UnitId, x.ReportingSystem, x.ReportingId))
            .ToListAsync();
        return Ok(new PagedResponse<PollutionRegisterResponse>(total, page, pageSize, items));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "CanRead")]
    [ProducesResponseType(typeof(PollutionRegisterResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PollutionRegisterResponse>> Get(Guid id)
    {
        var pollution = await _db.PollutionRegisters.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new PollutionRegisterResponse(x.Id, x.OrganisationId, x.ReportingPeriodId, x.LocationId, x.Substance, x.Quantity, x.UnitId, x.ReportingSystem, x.ReportingId))
            .FirstOrDefaultAsync();
        if (pollution is null) return NotFound();
        return Ok(pollution);
    }

    [HttpPost]
    [Authorize(Policy = "CanIngestData")]
    [ProducesResponseType(typeof(IdResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IdResponse>> Create([FromBody] PollutionRegisterRequest req)
    {
        if (req is null) return BadRequest();
        var p = new PollutionRegister
        {
            OrganisationId = req.OrganisationId,
            ReportingPeriodId = req.ReportingPeriodId,
            LocationId = req.LocationId,
            Substance = req.Substance,
            Quantity = req.Quantity,
            UnitId = req.UnitId,
            ReportingSystem = req.ReportingSystem,
            ReportingId = req.ReportingId
        };
        _db.Add(p);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = p.Id }, new IdResponse(p.Id));
    }
}
