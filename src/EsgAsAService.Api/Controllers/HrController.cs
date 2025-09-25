using EsgAsAService.Api.Models;
using EsgAsAService.Domain.Entities.Core;
using EsgAsAService.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EsgAsAService.Api.Controllers;

[ApiController]
[Route("hr")] 
public class HrController(EsgDbContext db) : ControllerBase
{
    private readonly EsgDbContext _db = db;

    [HttpGet("headcount")]
    [Authorize(Policy = "CanRead")]
    [ProducesResponseType(typeof(IReadOnlyList<HRHeadcountResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<HRHeadcountResponse>>> GetHeadcount([FromQuery] Guid periodId)
    {
        var items = await _db.HRHeadcounts.AsNoTracking()
            .Where(x => x.ReportingPeriodId == periodId)
            .Select(x => new HRHeadcountResponse(x.Id, x.OrganisationId, x.ReportingPeriodId, x.FteTotal, x.FteFemale, x.FteMale, x.FteOther, x.CountryCode))
            .ToListAsync();
        return Ok(items);
    }

    [HttpPost("headcount")]
    [Authorize(Policy = "CanIngestData")]
    [ProducesResponseType(typeof(IdResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IdResponse>> UpsertHeadcount([FromBody] HRHeadcountRequest req)
    {
        if (req is null) return BadRequest();
        var hc = await _db.HRHeadcounts.FirstOrDefaultAsync(x => x.ReportingPeriodId == req.ReportingPeriodId && x.OrganisationId == req.OrganisationId && x.CountryCode == req.CountryCode);
        if (hc is null)
        {
            hc = new HRHeadcount { OrganisationId = req.OrganisationId, ReportingPeriodId = req.ReportingPeriodId, CountryCode = req.CountryCode };
            _db.Add(hc);
        }
        hc.FteTotal = req.FteTotal; hc.FteFemale = req.FteFemale; hc.FteMale = req.FteMale; hc.FteOther = req.FteOther;
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetHeadcount), new { periodId = hc.ReportingPeriodId }, new IdResponse(hc.Id));
    }

    [HttpGet("payroll")]
    [Authorize(Policy = "CanRead")]
    [ProducesResponseType(typeof(IReadOnlyList<HRPayrollResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<HRPayrollResponse>>> GetPayroll([FromQuery] Guid periodId)
    {
        var items = await _db.HRPayrolls.AsNoTracking()
            .Where(x => x.ReportingPeriodId == periodId)
            .Select(x => new HRPayrollResponse(x.Id, x.OrganisationId, x.ReportingPeriodId, x.AvgSalaryFemale, x.AvgSalaryMale, x.CollectiveAgreementCoveragePct))
            .ToListAsync();
        return Ok(items);
    }

    [HttpPost("payroll")]
    [Authorize(Policy = "CanIngestData")]
    [ProducesResponseType(typeof(IdResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IdResponse>> UpsertPayroll([FromBody] HRPayrollRequest req)
    {
        if (req is null) return BadRequest();
        var pr = await _db.HRPayrolls.FirstOrDefaultAsync(x => x.ReportingPeriodId == req.ReportingPeriodId && x.OrganisationId == req.OrganisationId);
        if (pr is null)
        {
            pr = new HRPayroll { OrganisationId = req.OrganisationId, ReportingPeriodId = req.ReportingPeriodId };
            _db.Add(pr);
        }
        pr.AvgSalaryFemale = req.AvgSalaryFemale; pr.AvgSalaryMale = req.AvgSalaryMale; pr.CollectiveAgreementCoveragePct = req.CollectiveAgreementCoveragePct;
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetPayroll), new { periodId = pr.ReportingPeriodId }, new IdResponse(pr.Id));
    }

    [HttpGet("training")]
    [Authorize(Policy = "CanRead")]
    [ProducesResponseType(typeof(IReadOnlyList<HRTrainingResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<HRTrainingResponse>>> GetTraining([FromQuery] Guid periodId)
    {
        var items = await _db.HRTrainings.AsNoTracking()
            .Where(x => x.ReportingPeriodId == periodId)
            .Select(x => new HRTrainingResponse(x.Id, x.OrganisationId, x.ReportingPeriodId, x.TotalTrainingHours))
            .ToListAsync();
        return Ok(items);
    }

    [HttpPost("training")]
    [Authorize(Policy = "CanIngestData")]
    [ProducesResponseType(typeof(IdResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IdResponse>> UpsertTraining([FromBody] HRTrainingRequest req)
    {
        if (req is null) return BadRequest();
        var tr = await _db.HRTrainings.FirstOrDefaultAsync(x => x.ReportingPeriodId == req.ReportingPeriodId && x.OrganisationId == req.OrganisationId);
        if (tr is null)
        {
            tr = new HRTraining { OrganisationId = req.OrganisationId, ReportingPeriodId = req.ReportingPeriodId };
            _db.Add(tr);
        }
        tr.TotalTrainingHours = req.TotalTrainingHours;
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetTraining), new { periodId = tr.ReportingPeriodId }, new IdResponse(tr.Id));
    }
}
