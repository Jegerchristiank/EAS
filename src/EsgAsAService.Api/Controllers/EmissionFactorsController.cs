using EsgAsAService.Api.Auth;
using EsgAsAService.Api.Models;
using EsgAsAService.Domain.Entities.Core;
using EsgAsAService.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EsgAsAService.Api.Controllers;

[ApiController]
[Route("emission-factors")]
public class EmissionFactorsController : ControllerBase
{
    private readonly EsgDbContext _db;
    public EmissionFactorsController(EsgDbContext db) => _db = db;

    [HttpGet]
    [Authorize(Policy = "CanRead")]
    [ProducesResponseType(typeof(PagedResponse<EmissionFactorResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<EmissionFactorResponse>>> Search([FromQuery] string country, [FromQuery] int year, [FromQuery] string type, [FromQuery] int page = 1, [FromQuery] int pageSize = 100)
    {
        pageSize = Math.Clamp(pageSize, 1, 500);
        var query = _db.EmissionFactorsV2.AsNoTracking()
            .Where(f => f.Country == country && f.Year == year && f.Type == type);
        var total = await query.CountAsync();
        var items = await query.OrderByDescending(f => f.ValidFrom)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(f => new EmissionFactorResponse(f.Id, f.Country, f.Year, f.Type, f.UnitId, f.Value, f.ValidFrom, f.ValidTo, f.DataSourceId))
            .ToListAsync();
        return Ok(new PagedResponse<EmissionFactorResponse>(total, page, pageSize, items));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "CanRead")]
    [ProducesResponseType(typeof(EmissionFactorResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EmissionFactorResponse>> Get(Guid id)
    {
        var factor = await _db.EmissionFactorsV2.AsNoTracking()
            .Where(f => f.Id == id)
            .Select(f => new EmissionFactorResponse(f.Id, f.Country, f.Year, f.Type, f.UnitId, f.Value, f.ValidFrom, f.ValidTo, f.DataSourceId))
            .FirstOrDefaultAsync();
        if (factor is null) return NotFound();
        return Ok(factor);
    }

    [HttpPost]
    [Authorize(Policy = "CanManageReferenceData")]
    [ProducesResponseType(typeof(IdResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IdResponse>> Create([FromBody] EmissionFactorRequest req)
    {
        if (req is null) return BadRequest();
        var ef = new EmissionFactorV2
        {
            Country = req.Country,
            Year = req.Year,
            Type = req.Type,
            UnitId = req.UnitId,
            Value = req.Value,
            ValidFrom = req.ValidFrom,
            ValidTo = req.ValidTo,
            DataSourceId = req.DataSourceId
        };
        _db.Add(ef);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = ef.Id }, new IdResponse(ef.Id));
    }
}
