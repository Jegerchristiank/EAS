using EsgAsAService.Api.Models;
using EsgAsAService.Domain.Entities.Core;
using EsgAsAService.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EsgAsAService.Api.Controllers;

[ApiController]
[Route("financials")]
public class FinancialsController(EsgDbContext db) : ControllerBase
{
    private readonly EsgDbContext _db = db;

    [HttpGet]
    [Authorize(Policy = "CanRead")]
    public async Task<IActionResult> Get([FromQuery] Guid periodId)
    {
        var f = await _db.Financials.FirstOrDefaultAsync(x => x.ReportingPeriodId == periodId);
        if (f is null) return NotFound();
        return Ok(f);
    }

    [HttpPost]
    [Authorize(Policy = "CanIngestData")]
    public async Task<ActionResult<IdResponse>> Upsert([FromBody] FinancialsRequest req)
    {
        if (req is null) return BadRequest();
        var f = await _db.Financials.FirstOrDefaultAsync(x => x.ReportingPeriodId == req.ReportingPeriodId && x.OrganisationId == req.OrganisationId);
        var created = false;
        if (f is null)
        {
            f = new Financials { OrganisationId = req.OrganisationId, ReportingPeriodId = req.ReportingPeriodId };
            _db.Add(f);
            created = true;
        }
        f.Revenue = req.Revenue;
        f.Currency = req.Currency;
        await _db.SaveChangesAsync();
        if (created)
        {
            return CreatedAtAction(nameof(Get), new { periodId = f.ReportingPeriodId }, new IdResponse(f.Id));
        }

        return Ok(new IdResponse(f.Id));
    }
}
