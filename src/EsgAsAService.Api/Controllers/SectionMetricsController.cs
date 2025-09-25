using EsgAsAService.Api.Models;
using EsgAsAService.Domain.Entities.Core;
using EsgAsAService.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EsgAsAService.Api.Controllers;

[ApiController]
[Route("metrics")]
public class SectionMetricsController(EsgDbContext db) : ControllerBase
{
    private readonly EsgDbContext _db = db;

    [HttpGet("{periodId:guid}")]
    [Authorize(Policy = "CanRead")]
    public async Task<ActionResult<SectionMetricsResponse>> Get(Guid periodId, CancellationToken ct)
    {
        var period = await _db.ReportingPeriodsV2.AsNoTracking().FirstOrDefaultAsync(p => p.Id == periodId, ct);
        if (period is null) return NotFound();

        var metrics = await _db.SectionMetricInputs.AsNoTracking()
            .Where(x => x.ReportingPeriodId == periodId)
            .OrderBy(x => x.Section)
            .ThenBy(x => x.Metric)
            .Select(x => new SectionMetricDto(x.Section, x.Metric, x.NumericValue, x.TextValue, x.Unit, x.Notes))
            .ToListAsync(ct);

        return Ok(new SectionMetricsResponse(metrics));
    }

    [HttpPut("{periodId:guid}")]
    [Authorize(Policy = "CanIngestData")]
    public async Task<IActionResult> Upsert(Guid periodId, [FromBody] SectionMetricsUpsertRequest request, CancellationToken ct)
    {
        if (request is null) return BadRequest();
        var period = await _db.ReportingPeriodsV2.FirstOrDefaultAsync(p => p.Id == periodId, ct);
        if (period is null) return NotFound();

        foreach (var entry in request.Metrics)
        {
            var section = entry.Section.Trim();
            var metric = entry.Metric.Trim();
            if (string.IsNullOrWhiteSpace(section) || string.IsNullOrWhiteSpace(metric))
                continue;

            var existing = await _db.SectionMetricInputs.FirstOrDefaultAsync(x =>
                    x.ReportingPeriodId == periodId &&
                    x.Section == section &&
                    x.Metric == metric,
                ct);

            if (existing is null)
            {
                existing = new SectionMetricInput
                {
                    OrganisationId = period.OrganisationId,
                    ReportingPeriodId = periodId,
                    Section = section,
                    Metric = metric
                };
                await _db.SectionMetricInputs.AddAsync(existing, ct);
            }

            existing.NumericValue = entry.Value;
            existing.TextValue = string.IsNullOrWhiteSpace(entry.Text) ? null : entry.Text;
            existing.Unit = string.IsNullOrWhiteSpace(entry.Unit) ? null : entry.Unit;
            existing.Notes = string.IsNullOrWhiteSpace(entry.Notes) ? null : entry.Notes;
        }

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}
