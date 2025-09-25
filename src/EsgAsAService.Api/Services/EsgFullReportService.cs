using EsgAsAService.Application.Models;
using EsgAsAService.Infrastructure.Data;
using EsgAsAService.Infrastructure.Reporting;
using Microsoft.EntityFrameworkCore;

namespace EsgAsAService.Api.Services;

public interface IEsgFullReportService
{
    Task<FullEsgReport> GenerateAsync(Guid periodId, CancellationToken ct = default);
}

public class EsgFullReportService(EsgDbContext db) : IEsgFullReportService
{
    private readonly EsgDbContext _db = db;

    public async Task<FullEsgReport> GenerateAsync(Guid periodId, CancellationToken ct = default)
    {
        var period = await _db.ReportingPeriodsV2.FirstOrDefaultAsync(p => p.Id == periodId, ct);
        if (period is null) throw new InvalidOperationException("period_not_found");
        var org = await _db.Organisations.FirstAsync(o => o.Id == period.OrganisationId, ct);

        // B1 ground + policies
        var locations = await _db.Locations.Where(l => l.OrganisationId == org.Id).ToListAsync(ct);
        var policies = await _db.PolicyRegisters.FirstOrDefaultAsync(p => p.OrganisationId == org.Id, ct);
        var certs = await _db.Certificates.Where(c => c.OrganisationId == org.Id).ToListAsync(ct);

        // Payload by sections
        var result = new FullEsgReport
        {
            Meta = new MetaSection
            {
                Period = new PeriodMeta { Id = period.Id, Year = period.Year, StartDate = period.StartDate, EndDate = period.EndDate },
                Organisation = new OrganisationMeta { Id = org.Id, Name = org.Name }
            },
            B1 = new B1Section
            {
                LegalForm = null,
                NaceCode = null,
                Cvr = org.OrganizationNumber,
                Locations = locations.Select(l => new B1Location
                {
                    Id = l.Id,
                    Name = l.Name,
                    Latitude = l.Latitude,
                    Longitude = l.Longitude,
                    InSensitiveArea = l.InSensitiveArea,
                    Note = l.SensitiveAreaNote
                }).ToList(),
                Certificates = certs.Select(c => new B1Certificate { Standard = c.Standard, ValidFrom = c.ValidFrom, ValidTo = c.ValidTo }).ToList(),
                ConfidentialityOmissions = policies?.ConfidentialityOmissions ?? false,
                OmissionNote = policies?.OmissionNote
            },
            B2 = policies is null ? null : new B2Section
            {
                PolicyClimate = policies.PolicyClimate,
                PolicyEnvironment = policies.PolicyEnvironment,
                PolicyCircular = policies.PolicyCircular,
                PolicySupplyChain = policies.PolicySupplyChain,
                PolicyAntiCorruption = policies.PolicyAntiCorruption,
                PolicyDataPrivacy = policies.PolicyDataPrivacy,
                PolicyWhistleblower = policies.PolicyWhistleblower,
                Goal = policies.Goal,
                Status = policies.Status,
                NextMilestone = policies.NextMilestone
            },
            B3 = new B3Section(),
            B4 = new List<B4PollutionItem>(),
            B5 = locations.Select(l => new B5BiodiversityItem { Id = l.Id, InSensitiveArea = l.InSensitiveArea, SensitiveAreaNote = l.SensitiveAreaNote }).ToList()
        };

        await FullReportEnricher.PopulateFromV2Async(_db, result, periodId, ct);

        return result;
    }
}
