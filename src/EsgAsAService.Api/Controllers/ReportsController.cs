using EsgAsAService.Api.Auth;
using EsgAsAService.Domain.Entities.Core;
using EsgAsAService.Api.Services;
using EsgAsAService.Api.Models.Vsme;
using EsgAsAService.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
// (removed duplicate using)

namespace EsgAsAService.Api.Controllers;

[ApiController]
[Route("reports")] 
public class ReportsController : ControllerBase
{
    private readonly EsgDbContext _db;
    private readonly IVsmeReportService _vsme;
    private readonly IEsgFullReportService? _full; // optional, for JSON full report
    public ReportsController(EsgDbContext db, IVsmeReportService vsme, IEsgFullReportService? full = null) { _db = db; _vsme = vsme; _full = full; }

    /// <summary>
    /// Generates a VSME Basic report payload for a given period.
    /// </summary>
    [HttpPost("vsme/basic/generate")]
    [Authorize(Policy = "CanRead")]
    public async Task<IActionResult> GenerateVsmeBasic([FromQuery] Guid periodId)
    {
        var period = await _db.ReportingPeriodsV2.FirstOrDefaultAsync(p => p.Id == periodId);
        if (period == null) return NotFound();

        var report = await _vsme.GenerateBasicAsync(periodId);
        return Ok(new { period = periodId, report });
    }

    [HttpPost("export/pdf")]
    [Authorize(Policy = "CanRead")]
    public IActionResult ExportPdf([FromBody] object payload)
    {
        // In a full impl., render with QuestPDF. Here return bytes placeholder.
        var bytes = System.Text.Encoding.UTF8.GetBytes("PDF not implemented");
        return File(bytes, "application/pdf", "report.pdf");
    }

    [HttpPost("export/xbrl")]
    [Authorize(Policy = "CanRead")]
    public IActionResult ExportXbrl([FromBody] VsmeBasicReport report)
    {
        if (report is null) return BadRequest();
        static string FormatInvariant(object? v)
            => v is IFormattable f ? f.ToString(null, CultureInfo.InvariantCulture) : (v?.ToString() ?? string.Empty);

        var sb = new System.Text.StringBuilder();
        sb.Append("<xbrl>");
        sb.Append("<E1_TotalEmissions>");
        sb.Append(System.Security.SecurityElement.Escape(FormatInvariant(report.E1TotalEmissions.Value)));
        sb.Append("</E1_TotalEmissions>");
        sb.Append("<E4_Scope1>");
        sb.Append(System.Security.SecurityElement.Escape(FormatInvariant(report.E4Scope1.Value)));
        sb.Append("</E4_Scope1>");
        sb.Append("<E5_Scope2>");
        sb.Append(System.Security.SecurityElement.Escape(FormatInvariant(report.E5Scope2.Value)));
        sb.Append("</E5_Scope2>");
        sb.Append("<E6_Scope3>");
        sb.Append(System.Security.SecurityElement.Escape(FormatInvariant(report.E6Scope3.Value)));
        sb.Append("</E6_Scope3>");
        sb.Append("</xbrl>");
        var xml = sb.ToString();
        var bytes = System.Text.Encoding.UTF8.GetBytes(xml);
        return File(bytes, "application/xml", "report.xbrl");
    }

    /// <summary>
    /// Generates the full ESG JSON report for a period (B1–B11, C placeholders).
    /// Feature-gated via FeatureManagement key <c>FullEsgJson</c>.
    /// </summary>
    [HttpPost("generate/json")]
    [FeatureGate("FullEsgJson")]
    [Authorize(Policy = "CanRead")]
    public async Task<IActionResult> GenerateJson([FromQuery] Guid periodId)
    {
        // Why: The full report service is optional. Return 501 if not registered to make
        // capability clear to API consumers instead of a null reference.
        if (_full is null) return StatusCode(501, new { error = "full_report_service_not_registered" });
        // Why: explicit 404 clarifies input error and avoids silently generating an empty payload.
        var exists = await _db.ReportingPeriodsV2.AnyAsync(p => p.Id == periodId);
        if (!exists) return NotFound();
        var payload = await _full.GenerateAsync(periodId);
        return Ok(payload);
    }
}
