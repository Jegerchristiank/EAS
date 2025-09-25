using EsgAsAService.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EsgAsAService.Api.Controllers;

[ApiController]
[Route("calculation-results")]
public class CalculationResultsController : ControllerBase
{
    private readonly EsgDbContext _db;
    public CalculationResultsController(EsgDbContext db) => _db = db;

    [HttpGet]
    [Authorize(Policy = "CanRead")]
    public async Task<IActionResult> List([FromQuery] Guid periodId, [FromQuery] int page = 1, [FromQuery] int pageSize = 200)
    {
        pageSize = Math.Clamp(pageSize, 1, 1000);
        var query = _db.CalculationResults.AsNoTracking()
            .Where(r => _db.ScopeEntries.Any(se => se.Id == r.ScopeEntryId && _db.Activities.Any(a => a.Id == se.ActivityId && a.ReportingPeriodId == periodId)));
        var total = await query.CountAsync();
        var items = await query.OrderBy(r => r.ScopeEntryId)
            .Skip((page-1)*pageSize).Take(pageSize).ToListAsync();
        return Ok(new { total, page, pageSize, items });
    }
}
