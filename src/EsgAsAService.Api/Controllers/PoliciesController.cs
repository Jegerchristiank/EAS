using EsgAsAService.Api.Models;
using EsgAsAService.Domain.Entities.Core;
using EsgAsAService.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EsgAsAService.Api.Controllers;

[ApiController]
[Route("policies")]
public class PoliciesController(EsgDbContext db) : ControllerBase
{
    private readonly EsgDbContext _db = db;

    [HttpGet("{orgId:guid}")]
    [Authorize(Policy = "CanRead")]
    public async Task<IActionResult> Get(Guid orgId)
    {
        var p = await _db.PolicyRegisters.FirstOrDefaultAsync(x => x.OrganisationId == orgId);
        if (p is null) return NotFound();
        return Ok(p);
    }

    [HttpPut("{orgId:guid}")]
    [Authorize(Policy = "CanIngestData")]
    public async Task<IActionResult> Upsert(Guid orgId, [FromBody] PolicyRegisterRequest req)
    {
        if (req is null || req.OrganisationId != orgId) return BadRequest();
        var existing = await _db.PolicyRegisters.FirstOrDefaultAsync(x => x.OrganisationId == orgId);
        if (existing is null)
        {
            existing = new PolicyRegister { OrganisationId = orgId };
            _db.Add(existing);
        }
        existing.PolicyClimate = req.PolicyClimate;
        existing.PolicyEnvironment = req.PolicyEnvironment;
        existing.PolicyCircular = req.PolicyCircular;
        existing.PolicySupplyChain = req.PolicySupplyChain;
        existing.PolicyAntiCorruption = req.PolicyAntiCorruption;
        existing.PolicyDataPrivacy = req.PolicyDataPrivacy;
        existing.PolicyWhistleblower = req.PolicyWhistleblower;
        existing.Goal = req.Goal;
        existing.Status = req.Status;
        existing.NextMilestone = req.NextMilestone;
        existing.ConfidentialityOmissions = req.ConfidentialityOmissions;
        existing.OmissionNote = req.OmissionNote;
        await _db.SaveChangesAsync();
        return Ok(new IdResponse(existing.Id));
    }
}

