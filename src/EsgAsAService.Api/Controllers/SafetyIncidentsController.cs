using EsgAsAService.Api.Models;
using EsgAsAService.Domain.Entities.Core;
using EsgAsAService.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EsgAsAService.Api.Controllers;

[ApiController]
[Route("safety/incidents")] 
public class SafetyIncidentsController(EsgDbContext db) : ControllerBase
{
    private readonly EsgDbContext _db = db;

    [HttpGet]
    [Authorize(Policy = "CanRead")]
    public async Task<IActionResult> List([FromQuery] Guid periodId)
        => Ok(await _db.SafetyIncidents.Where(x => x.ReportingPeriodId == periodId).ToListAsync());

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "CanRead")]
    public async Task<ActionResult<SafetyIncident>> Get(Guid id)
    {
        var incident = await _db.SafetyIncidents.FindAsync(id);
        if (incident is null) return NotFound();
        return incident;
    }

    [HttpPost]
    [Authorize(Policy = "CanIngestData")]
    public async Task<ActionResult<IdResponse>> Create([FromBody] SafetyIncidentRequest req)
    {
        if (req is null) return BadRequest();
        var incident = new SafetyIncident
        {
            OrganisationId = req.OrganisationId,
            ReportingPeriodId = req.ReportingPeriodId,
            IncidentsCount = req.IncidentsCount,
            HoursWorked = req.HoursWorked,
            SourceDoc = req.SourceDoc
        };
        _db.Add(incident);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = incident.Id }, new IdResponse(incident.Id));
    }
}
