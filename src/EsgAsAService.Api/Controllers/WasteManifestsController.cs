using EsgAsAService.Api.Models;
using EsgAsAService.Domain.Entities.Core;
using EsgAsAService.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EsgAsAService.Api.Controllers;

[ApiController]
[Route("waste/manifests")]
public class WasteManifestsController(EsgDbContext db) : ControllerBase
{
    private readonly EsgDbContext _db = db;

    [HttpGet]
    [Authorize(Policy = "CanRead")]
    [ProducesResponseType(typeof(PagedResponse<WasteManifestResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<WasteManifestResponse>>> List([FromQuery] Guid periodId, [FromQuery] int page = 1, [FromQuery] int pageSize = 100)
    {
        pageSize = Math.Clamp(pageSize, 1, 500);
        var query = _db.WasteManifests.AsNoTracking().Where(x => periodId == Guid.Empty || x.ReportingPeriodId == periodId);
        var total = await query.CountAsync();
        var items = await query.OrderBy(x => x.EakCode)
            .ThenBy(x => x.LocationId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new WasteManifestResponse(x.Id, x.OrganisationId, x.ReportingPeriodId, x.LocationId, x.EakCode, x.QuantityKg, x.Disposition, x.Carrier, x.ManifestId))
            .ToListAsync();
        return Ok(new PagedResponse<WasteManifestResponse>(total, page, pageSize, items));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "CanRead")]
    [ProducesResponseType(typeof(WasteManifestResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WasteManifestResponse>> Get(Guid id)
    {
        var manifest = await _db.WasteManifests.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new WasteManifestResponse(x.Id, x.OrganisationId, x.ReportingPeriodId, x.LocationId, x.EakCode, x.QuantityKg, x.Disposition, x.Carrier, x.ManifestId))
            .FirstOrDefaultAsync();
        if (manifest is null) return NotFound();
        return Ok(manifest);
    }

    [HttpPost]
    [Authorize(Policy = "CanIngestData")]
    [ProducesResponseType(typeof(IdResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IdResponse>> Create([FromBody] WasteManifestRequest req)
    {
        if (req is null) return BadRequest();
        var w = new WasteManifest
        {
            OrganisationId = req.OrganisationId,
            ReportingPeriodId = req.ReportingPeriodId,
            LocationId = req.LocationId,
            EakCode = req.EakCode,
            QuantityKg = req.QuantityKg,
            Disposition = req.Disposition,
            Carrier = req.Carrier,
            ManifestId = req.ManifestId
        };
        _db.Add(w);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = w.Id }, new IdResponse(w.Id));
    }
}
