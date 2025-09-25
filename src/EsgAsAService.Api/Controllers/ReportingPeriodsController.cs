using EsgAsAService.Api.Auth;
using EsgAsAService.Api.Models;
using EsgAsAService.Domain.Entities.Core;
using EsgAsAService.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EsgAsAService.Api.Controllers;

[ApiController]
[Route("reporting-periods")]
public class ReportingPeriodsController : ControllerBase
{
    private readonly EsgDbContext _db;
    public ReportingPeriodsController(EsgDbContext db) => _db = db;

    [HttpGet]
    [Authorize(Policy = "CanRead")]
    public async Task<IActionResult> List([FromQuery] Guid organisationId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        pageSize = Math.Clamp(pageSize, 1, 200);
        var query = _db.ReportingPeriodsV2.AsNoTracking()
            .Where(r => organisationId == Guid.Empty || r.OrganisationId == organisationId);
        var total = await query.CountAsync();
        var items = await query.OrderByDescending(r => r.Year)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return Ok(new { total, page, pageSize, items });
    }

    [HttpPost]
    [Authorize(Policy = "CanIngestData")]
    public async Task<ActionResult<IdResponse>> Create([FromBody] ReportingPeriodRequest req)
    {
        if (req is null) return BadRequest();
        var rp = new ReportingPeriodV2
        {
            OrganisationId = req.OrganisationId,
            Year = req.Year,
            StartDate = req.StartDate,
            EndDate = req.EndDate
        };
        _db.Add(rp);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = rp.Id }, new IdResponse(rp.Id));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "CanRead")]
    public async Task<IActionResult> Get(Guid id)
    {
        var rp = await _db.ReportingPeriodsV2.FindAsync(id);
        if (rp == null) return NotFound();
        return Ok(rp);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "CanIngestData")]
    public async Task<ActionResult<IdResponse>> Update(Guid id, [FromBody] ReportingPeriodRequest req)
    {
        if (req is null) return BadRequest();
        var rp = await _db.ReportingPeriodsV2.FindAsync(id);
        if (rp == null) return NotFound();
        rp.OrganisationId = req.OrganisationId;
        rp.Year = req.Year;
        rp.StartDate = req.StartDate;
        rp.EndDate = req.EndDate;
        _db.Update(rp);
        await _db.SaveChangesAsync();
        return Ok(new IdResponse(rp.Id));
    }
}
