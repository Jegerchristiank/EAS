using EsgAsAService.Api.Auth;
using EsgAsAService.Api.Models;
using EsgAsAService.Domain.Entities.Core;
using EsgAsAService.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EsgAsAService.Api.Controllers;

[ApiController]
[Route("data-sources")]
public class DataSourcesController : ControllerBase
{
    private readonly EsgDbContext _db;
    public DataSourcesController(EsgDbContext db) => _db = db;

    [HttpGet]
    [Authorize(Policy = "CanRead")]
    public async Task<IActionResult> List([FromQuery] string? q)
        => Ok(await _db.DataSources.Where(d => q == null || d.Name.Contains(q)).OrderBy(d => d.Name).ToListAsync());

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "CanRead")]
    public async Task<IActionResult> Get(Guid id)
    {
        var ds = await _db.DataSources.FindAsync(id);
        if (ds is null) return NotFound();
        return Ok(ds);
    }

    [HttpPost]
    [Authorize(Policy = "CanManageReferenceData")]
    public async Task<ActionResult<IdResponse>> Create([FromBody] DataSource req)
    {
        if (req is null) return BadRequest();
        var ds = new DataSource { Name = req.Name, Description = req.Description, Url = req.Url };
        _db.Add(ds); await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = ds.Id }, new IdResponse(ds.Id));
    }
}
