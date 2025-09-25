using EsgAsAService.Api.Models;
using EsgAsAService.Domain.Entities.Core;
using EsgAsAService.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EsgAsAService.Api.Controllers;

[ApiController]
[Route("materials/flows")]
public class MaterialsController(EsgDbContext db) : ControllerBase
{
    private readonly EsgDbContext _db = db;

    [HttpGet]
    [Authorize(Policy = "CanRead")]
    public async Task<IActionResult> List([FromQuery] Guid periodId)
        => Ok(await _db.MaterialFlows.Where(x => periodId == Guid.Empty || x.ReportingPeriodId == periodId)
                                     .OrderByDescending(x => x.QuantityTonnes).ToListAsync());

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "CanRead")]
    public async Task<ActionResult<MaterialFlow>> Get(Guid id)
    {
        var flow = await _db.MaterialFlows.FindAsync(id);
        if (flow is null) return NotFound();
        return flow;
    }

    [HttpPost]
    [Authorize(Policy = "CanIngestData")]
    public async Task<ActionResult<IdResponse>> Create([FromBody] MaterialFlowRequest req)
    {
        if (req is null) return BadRequest();
        var flow = new MaterialFlow
        {
            OrganisationId = req.OrganisationId,
            ReportingPeriodId = req.ReportingPeriodId,
            Material = req.Material,
            QuantityTonnes = req.QuantityTonnes,
            SourceDoc = req.SourceDoc
        };
        _db.Add(flow);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = flow.Id }, new IdResponse(flow.Id));
    }
}
