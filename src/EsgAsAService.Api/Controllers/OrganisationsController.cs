using EsgAsAService.Api.Auth;
using EsgAsAService.Api.Models;
using EsgAsAService.Domain.Entities.Core;
using EsgAsAService.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EsgAsAService.Api.Controllers;

[ApiController]
[Route("organisations")]
public class OrganisationsController : ControllerBase
{
    private readonly EsgDbContext _db;
    public OrganisationsController(EsgDbContext db) => _db = db;

    [HttpGet]
    [Authorize(Policy = "CanRead")]
    public async Task<IActionResult> List([FromQuery] string? q, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        pageSize = Math.Clamp(pageSize, 1, 200);
        var query = _db.Organisations.AsNoTracking()
            .Where(o => q == null || o.Name.Contains(q));
        var total = await query.CountAsync();
        var items = await query.OrderBy(o => o.Name)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return Ok(new { total, page, pageSize, items });
    }

    [HttpPost]
    [Authorize(Policy = "CanIngestData")]
    public async Task<ActionResult<IdResponse>> Create([FromBody] OrganisationRequest req)
    {
        if (req is null) return BadRequest();
        var org = new Organisation
        {
            Name = req.Name,
            Industry = req.Industry,
            CountryCode = req.CountryCode,
            OrganizationNumber = req.OrganizationNumber
        };
        _db.Add(org);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = org.Id }, new IdResponse(org.Id));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "CanRead")]
    public async Task<IActionResult> Get(Guid id)
    {
        var org = await _db.Organisations.FindAsync(id);
        if (org == null) return NotFound();
        return Ok(org);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "CanIngestData")]
    public async Task<ActionResult<IdResponse>> Update(Guid id, [FromBody] OrganisationRequest req)
    {
        if (req is null) return BadRequest();
        var existing = await _db.Organisations.FindAsync(id);
        if (existing == null) return NotFound();

        existing.Name = req.Name;
        existing.Industry = req.Industry;
        existing.CountryCode = req.CountryCode;
        existing.OrganizationNumber = req.OrganizationNumber;
        _db.Update(existing); // revisioning will create new row
        await _db.SaveChangesAsync();
        return Ok(new IdResponse(existing.Id));
    }
}
