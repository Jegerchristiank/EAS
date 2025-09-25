using EsgAsAService.Api.Auth;
using EsgAsAService.Api.Models;
using EsgAsAService.Domain.Entities.Core;
using EsgAsAService.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EsgAsAService.Api.Controllers;

[ApiController]
[Route("units")]
public class UnitsController : ControllerBase
{
    private readonly EsgDbContext _db;
    public UnitsController(EsgDbContext db) => _db = db;

    [HttpGet]
    [Authorize(Policy = "CanRead")]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 100)
    {
        pageSize = Math.Clamp(pageSize, 1, 500);
        var query = _db.Units.AsNoTracking();
        var total = await query.CountAsync();
        var items = await query.OrderBy(u => u.Code).Skip((page-1)*pageSize).Take(pageSize).ToListAsync();
        return Ok(new { total, page, pageSize, items });
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "CanRead")]
    public async Task<IActionResult> Get(Guid id)
    {
        var unit = await _db.Units.FindAsync(id);
        if (unit is null) return NotFound();
        return Ok(unit);
    }

    [HttpPost]
    [Authorize(Policy = "CanManageReferenceData")]
    public async Task<ActionResult<IdResponse>> Create([FromBody] UnitRequest req)
    {
        if (req is null) return BadRequest();
        var unit = new Unit { Code = req.Code, Name = req.Name };
        _db.Add(unit); await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = unit.Id }, new IdResponse(unit.Id));
    }

    [HttpPost("conversions")]
    [Authorize(Policy = "CanManageReferenceData")]
    public async Task<ActionResult<IdResponse>> CreateConversion([FromBody] UnitConversionRequest req)
    {
        if (req is null) return BadRequest();
        var conversion = new UnitConversion { FromUnitId = req.FromUnitId, ToUnitId = req.ToUnitId, Factor = req.Factor };
        _db.Add(conversion);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetConversion), new { id = conversion.Id }, new IdResponse(conversion.Id));
    }

    [HttpGet("conversions/{id:guid}")]
    [Authorize(Policy = "CanRead")]
    public async Task<ActionResult<UnitConversion>> GetConversion(Guid id)
    {
        var conversion = await _db.UnitConversions.FindAsync(id);
        if (conversion is null) return NotFound();
        return conversion;
    }
}
