using EsgAsAService.Api.Models;
using EsgAsAService.Domain.Entities.Core;
using EsgAsAService.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EsgAsAService.Api.Controllers;

[ApiController]
[Route("water/meters")]
public class WaterMetersController(EsgDbContext db) : ControllerBase
{
    private readonly EsgDbContext _db = db;

    [HttpGet]
    [Authorize(Policy = "CanRead")]
    [ProducesResponseType(typeof(PagedResponse<WaterMeterResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<WaterMeterResponse>>> List([FromQuery] Guid periodId, [FromQuery] int page = 1, [FromQuery] int pageSize = 100)
    {
        pageSize = Math.Clamp(pageSize, 1, 500);
        var query = _db.WaterMeters.AsNoTracking().Where(x => periodId == Guid.Empty || x.ReportingPeriodId == periodId);
        var total = await query.CountAsync();
        var items = await query.OrderBy(x => x.ReportingPeriodId)
            .ThenBy(x => x.LocationId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new WaterMeterResponse(x.Id, x.OrganisationId, x.ReportingPeriodId, x.LocationId, x.IntakeM3, x.DischargeM3, x.SourceDoc))
            .ToListAsync();
        return Ok(new PagedResponse<WaterMeterResponse>(total, page, pageSize, items));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "CanRead")]
    [ProducesResponseType(typeof(WaterMeterResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WaterMeterResponse>> Get(Guid id)
    {
        var meter = await _db.WaterMeters.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new WaterMeterResponse(x.Id, x.OrganisationId, x.ReportingPeriodId, x.LocationId, x.IntakeM3, x.DischargeM3, x.SourceDoc))
            .FirstOrDefaultAsync();
        if (meter is null) return NotFound();
        return Ok(meter);
    }

    [HttpPost]
    [Authorize(Policy = "CanIngestData")]
    [ProducesResponseType(typeof(IdResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IdResponse>> Create([FromBody] WaterMeterRequest req)
    {
        if (req is null) return BadRequest();
        var m = new WaterMeter
        {
            OrganisationId = req.OrganisationId,
            ReportingPeriodId = req.ReportingPeriodId,
            LocationId = req.LocationId,
            IntakeM3 = req.IntakeM3,
            DischargeM3 = req.DischargeM3,
            SourceDoc = req.SourceDoc
        };
        _db.Add(m);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = m.Id }, new IdResponse(m.Id));
    }
}
