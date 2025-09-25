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
[Route("activities")]
public class ActivitiesController : ControllerBase
{
    private readonly EsgDbContext _db;
    public ActivitiesController(EsgDbContext db) => _db = db;

    /// <summary>
    /// Lists activities for a reporting period with optional free-text filter and paging.
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "CanRead")]
    [ProducesResponseType(typeof(PagedResponse<ActivityResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<ActivityResponse>>> List([FromQuery] Guid periodId, [FromQuery] string? q, [FromQuery] int page = 1, [FromQuery] int pageSize = 100)
    {
        pageSize = Math.Clamp(pageSize, 1, 500);
        var query = _db.Activities.AsNoTracking().Where(a => a.ReportingPeriodId == periodId);
        if (!string.IsNullOrWhiteSpace(q)) query = query.Where(a => a.Category.Contains(q));
        var total = await query.CountAsync();
        var items = await query.OrderBy(a => a.Category).ThenBy(a => a.ActivityDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new ActivityResponse(a.Id, a.OrganisationId, a.ReportingPeriodId, a.Category, a.ActivityDate, a.Quantity, a.UnitId, a.Description))
            .ToListAsync();
        return Ok(new PagedResponse<ActivityResponse>(total, page, pageSize, items));
    }

    /// <summary>
    /// Returns a single activity by identifier.
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = "CanRead")]
    [ProducesResponseType(typeof(ActivityResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ActivityResponse>> Get(Guid id)
    {
        var activity = await _db.Activities.AsNoTracking()
            .Where(a => a.Id == id)
            .Select(a => new ActivityResponse(a.Id, a.OrganisationId, a.ReportingPeriodId, a.Category, a.ActivityDate, a.Quantity, a.UnitId, a.Description))
            .FirstOrDefaultAsync();
        if (activity is null) return NotFound();
        return Ok(activity);
    }

    /// <summary>
    /// Creates a new activity under a reporting period.
    /// Returns 201 Created with Location header to <c>GET /activities/{{id}}</c>.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "CanIngestData")]
    [ProducesResponseType(typeof(IdResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IdResponse>> Create([FromBody] ActivityRequest req)
    {
        if (req is null) return BadRequest();
        var a = new Activity
        {
            OrganisationId = req.OrganisationId,
            ReportingPeriodId = req.ReportingPeriodId,
            Category = req.Category,
            ActivityDate = req.ActivityDate,
            Quantity = req.Quantity,
            UnitId = req.UnitId
        };
        _db.Add(a);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = a.Id }, new IdResponse(a.Id));
    }
}
