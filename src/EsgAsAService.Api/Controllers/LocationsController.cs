using EsgAsAService.Api.Models;
using EsgAsAService.Domain.Entities.Core;
using EsgAsAService.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EsgAsAService.Api.Controllers;

[ApiController]
[Route("locations")]
public class LocationsController(EsgDbContext db) : ControllerBase
{
    private readonly EsgDbContext _db = db;

    [HttpGet]
    [Authorize(Policy = "CanRead")]
    [ProducesResponseType(typeof(IReadOnlyList<LocationResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<LocationResponse>>> List([FromQuery] Guid orgId)
    {
        var items = await _db.Locations.AsNoTracking()
            .Where(l => orgId == Guid.Empty || l.OrganisationId == orgId)
            .OrderBy(l => l.Name)
            .Select(l => new LocationResponse(l.Id, l.OrganisationId, l.Name, l.Latitude, l.Longitude, l.InSensitiveArea, l.SensitiveAreaNote))
            .ToListAsync();
        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "CanRead")]
    [ProducesResponseType(typeof(LocationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LocationResponse>> Get(Guid id)
    {
        var location = await _db.Locations.AsNoTracking()
            .Where(l => l.Id == id)
            .Select(l => new LocationResponse(l.Id, l.OrganisationId, l.Name, l.Latitude, l.Longitude, l.InSensitiveArea, l.SensitiveAreaNote))
            .FirstOrDefaultAsync();
        if (location is null) return NotFound();
        return Ok(location);
    }

    [HttpPost]
    [Authorize(Policy = "CanIngestData")]
    [ProducesResponseType(typeof(IdResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IdResponse>> Create([FromBody] LocationRequest req)
    {
        if (req is null) return BadRequest();
        var l = new Location
        {
            OrganisationId = req.OrganisationId,
            Name = req.Name,
            Latitude = req.Latitude,
            Longitude = req.Longitude,
            InSensitiveArea = req.InSensitiveArea,
            SensitiveAreaNote = req.SensitiveAreaNote
        };
        _db.Add(l);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = l.Id }, new IdResponse(l.Id));
    }
}
