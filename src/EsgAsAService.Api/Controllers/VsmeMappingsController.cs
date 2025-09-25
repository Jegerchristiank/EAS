using EsgAsAService.Api.Models;
using EsgAsAService.Domain.Entities.Core;
using EsgAsAService.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EsgAsAService.Api.Controllers;

[ApiController]
[Route("vsme-mappings")]
public class VsmeMappingsController : ControllerBase
{
    private readonly EsgDbContext _db;
    public VsmeMappingsController(EsgDbContext db) => _db = db;

    [HttpGet]
    [Authorize(Policy = "CanRead")]
    [ProducesResponseType(typeof(IReadOnlyList<VsmeMappingResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<VsmeMappingResponse>>> List()
    {
        var items = await _db.VsmeMappings.AsNoTracking()
            .OrderByDescending(v => v.CreatedAt)
            .Select(v => new VsmeMappingResponse(v.Id, v.Json, v.CreatedAt))
            .ToListAsync();
        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "CanRead")]
    [ProducesResponseType(typeof(VsmeMappingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VsmeMappingResponse>> Get(Guid id)
    {
        var mapping = await _db.VsmeMappings.AsNoTracking()
            .Where(v => v.Id == id)
            .Select(v => new VsmeMappingResponse(v.Id, v.Json, v.CreatedAt))
            .FirstOrDefaultAsync();
        if (mapping is null) return NotFound();
        return Ok(mapping);
    }

    [HttpPost]
    [Authorize(Policy = "CanManageReferenceData")]
    [ProducesResponseType(typeof(IdResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IdResponse>> Create([FromBody] VsmeMapping req)
    {
        if (req is null) return BadRequest();
        var vm = new VsmeMapping { Json = req.Json };
        _db.Add(vm);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = vm.Id }, new IdResponse(vm.Id));
    }
}
