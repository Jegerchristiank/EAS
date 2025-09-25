using EsgAsAService.Api.Auth;
using EsgAsAService.Api.Models;
using EsgAsAService.Domain.Entities.Core;
using EsgAsAService.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EsgAsAService.Api.Controllers;

[ApiController]
[Route("approvals")]
public class ApprovalsController : ControllerBase
{
    private readonly EsgDbContext _db;
    public ApprovalsController(EsgDbContext db) => _db = db;

    [HttpPost("submit")]
    [Authorize(Policy = "CanCalculate")]
    public async Task<IActionResult> Submit([FromBody] ApprovalSubmitRequest req)
    {
        if (req is null) return BadRequest();
        var ap = new Approval { ReportingPeriodId = req.ReportingPeriodId, Status = "Under review" };
        _db.Add(ap); await _db.SaveChangesAsync();
        return Ok(new IdResponse(ap.Id));
    }

    [HttpPatch("{id:guid}")]
    [Authorize(Policy = "CanApprove")]
    public async Task<IActionResult> Patch(Guid id, [FromBody] ApprovalPatchRequest req)
    {
        if (req is null) return BadRequest();
        var ap = await _db.Approvals.FindAsync(id);
        if (ap == null) return NotFound();
        var status = req.Status;
        if ((string.Equals(status, "Approved", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(status, "Rejected", StringComparison.OrdinalIgnoreCase)) &&
            string.IsNullOrWhiteSpace(req.Comment))
        {
            return BadRequest(new[] { new { code = "validation_error", field = nameof(req.Comment), message = "Comment is required when approving or rejecting." } });
        }

        ap.Status = req.Status; ap.Comment = req.Comment; _db.Update(ap); await _db.SaveChangesAsync();
        return Ok(new IdResponse(ap.Id));
    }
}
