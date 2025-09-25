using EsgAsAService.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EsgAsAService.Api.Controllers;

[ApiController]
[Route("metrics")]
public class MetricsController(EsgDbContext db) : ControllerBase
{
    private readonly EsgDbContext _db = db;

    [HttpGet("emissions")]
    [Authorize(Policy = "CanRead")]
    public async Task<IActionResult> Emissions([FromQuery] Guid periodId)
    {
        // Prefer CalculationResults; fallback to 0 when not available
        var q = from cr in _db.CalculationResults
                join se in _db.ScopeEntries on cr.ScopeEntryId equals se.Id
                join act in _db.Activities on se.ActivityId equals act.Id
                where act.ReportingPeriodId == periodId
                select new { se.Scope, cr.Co2eKg };

        var data = await q.ToListAsync();
        double s1 = data.Where(x => x.Scope == 1).Sum(x => x.Co2eKg);
        double s2 = data.Where(x => x.Scope == 2).Sum(x => x.Co2eKg);
        double s3 = data.Where(x => x.Scope == 3).Sum(x => x.Co2eKg);
        double total = s1 + s2 + s3;

        var fin = await _db.Financials.FirstOrDefaultAsync(x => x.ReportingPeriodId == periodId);
        double? intensity = fin is null || fin.Revenue == 0 ? null : total / fin.Revenue;

        return Ok(new { scope1_kg = s1, scope2_kg = s2, scope3_kg = s3, total_kg = total, intensity_kg_per_revenue = intensity });
    }

    [HttpGet("water")]
    [Authorize(Policy = "CanRead")]
    public async Task<IActionResult> Water([FromQuery] Guid periodId)
    {
        var meters = await _db.WaterMeters.Where(x => x.ReportingPeriodId == periodId).ToListAsync();
        var intake = meters.Sum(x => x.IntakeM3);
        var discharge = meters.Sum(x => x.DischargeM3 ?? 0);
        var consumption = intake - discharge;
        return Ok(new { intake_m3 = intake, discharge_m3 = discharge, consumption_m3 = consumption });
    }

    [HttpGet("accidents")]
    [Authorize(Policy = "CanRead")]
    public async Task<IActionResult> Accidents([FromQuery] Guid periodId)
    {
        var s = await _db.SafetyIncidents.Where(x => x.ReportingPeriodId == periodId).ToListAsync();
        double incidents = s.Sum(x => (double)x.IncidentsCount);
        double hours = s.Sum(x => x.HoursWorked);
        double? afr = hours <= 0 ? null : (incidents / hours) * 200000.0;
        return Ok(new { incidents_count = incidents, hours_worked = hours, accident_frequency = afr });
    }

    [HttpGet("gender-pay-gap")]
    [Authorize(Policy = "CanRead")]
    public async Task<IActionResult> GenderPayGap([FromQuery] Guid periodId)
    {
        var p = await _db.HRPayrolls.FirstOrDefaultAsync(x => x.ReportingPeriodId == periodId);
        if (p is null || !p.AvgSalaryFemale.HasValue || !p.AvgSalaryMale.HasValue || p.AvgSalaryMale == 0)
            return Ok(new { gender_pay_gap = (double?)null });
        double gap = (p.AvgSalaryFemale.Value / p.AvgSalaryMale.Value) - 1.0;
        return Ok(new { gender_pay_gap = gap });
    }
}

