using EsgAsAService.Api.Auth;
using EsgAsAService.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EsgAsAService.Api.Controllers;

[ApiController]
[Route("audit-logs")] 
public class AuditLogsController : ControllerBase
{
    private readonly EsgDbContext _db;
    public AuditLogsController(EsgDbContext db) => _db = db;

    [HttpGet]
    [Authorize(Policy = "CanRead")]
    public async Task<IActionResult> List([FromQuery] string? entity, [FromQuery] int page = 1, [FromQuery] int pageSize = 200)
    {
        pageSize = Math.Clamp(pageSize, 1, 1000);
        var q = _db.AuditLogs.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(entity)) q = q.Where(a => a.EntityName == entity);
        var total = await q.CountAsync();
        var items = await q.OrderByDescending(a => a.Timestamp)
            .Skip((page-1)*pageSize).Take(pageSize).ToListAsync();
        return Ok(new { total, page, pageSize, items });
    }
}
