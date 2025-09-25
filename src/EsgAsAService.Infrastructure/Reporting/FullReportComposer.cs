using EsgAsAService.Application.Abstractions;
using EsgAsAService.Application.Models;
using EsgAsAService.Domain.Entities;
using EsgAsAService.Domain.Entities.Core;
using EsgAsAService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EsgAsAService.Infrastructure.Reporting;

public class FullReportComposer : IFullReportComposer
{
    private readonly EsgDbContext _db;
    public FullReportComposer(EsgDbContext db) => _db = db;

    public async Task<FullEsgReport> ComposeAsync(Company companyV1, ReportingPeriod periodV1, CancellationToken ct = default)
    {
        Guid? periodV2Id = null;
        // 1) Use explicit mapping first (avoid touching V2 tables when not configured)
        var map = await _db.PeriodMappings.AsNoTracking().FirstOrDefaultAsync(m => m.V1PeriodId == periodV1.Id, ct);
        if (map is not null)
        {
            var exists = await _db.ReportingPeriodsV2.AsNoTracking().AnyAsync(p => p.Id == map.V2PeriodId, ct);
            if (exists) periodV2Id = map.V2PeriodId;
        }
        // 2) Best-effort guess only if no mapping (guarded to avoid exceptions when V2 schema missing)
        EsgAsAService.Domain.Entities.Core.Organisation? org = null;
        if (periodV2Id is null)
        {
            try
            {
                var orgQuery = _db.Organisations.AsNoTracking().Where(o => o.IsActive);
                if (!string.IsNullOrEmpty(companyV1.OrganizationNumber))
                    orgQuery = orgQuery.Where(o => o.OrganizationNumber == companyV1.OrganizationNumber);
                else
                    orgQuery = orgQuery.Where(o => o.Name == companyV1.Name);

                org = await orgQuery.FirstOrDefaultAsync(ct);
                if (org is not null)
                {
                    var v2 = await _db.ReportingPeriodsV2.AsNoTracking()
                        .FirstOrDefaultAsync(p => p.OrganisationId == org.Id && p.Year == periodV1.Year, ct);
                    periodV2Id = v2?.Id;
                }
            }
            catch
            {
                // ignore when V2 tables don't exist
            }
        }

        var rep = new FullEsgReport
        {
            Meta = new MetaSection
            {
                Period = new PeriodMeta { Id = periodV1.Id, Year = periodV1.Year, StartDate = periodV1.StartDate, EndDate = periodV1.EndDate },
                Organisation = new OrganisationMeta { Id = org?.Id ?? Guid.Empty, Name = org?.Name ?? companyV1.Name }
            },
            B1 = new B1Section
            {
                Cvr = companyV1.OrganizationNumber,
                Locations = org is null ? new() : await _db.Locations.Where(l => l.OrganisationId == org.Id).Select(l => new B1Location
                {
                    Id = l.Id,
                    Name = l.Name,
                    Latitude = l.Latitude,
                    Longitude = l.Longitude,
                    InSensitiveArea = l.InSensitiveArea,
                    Note = l.SensitiveAreaNote
                }).ToListAsync(ct),
                Certificates = org is null ? new() : await _db.Certificates.Where(c => c.OrganisationId == org.Id)
                    .Select(c => new B1Certificate { Standard = c.Standard, ValidFrom = c.ValidFrom, ValidTo = c.ValidTo }).ToListAsync(ct)
            }
        };

        // Policies (B1 omissions + B2 section)
        if (org is not null)
        {
            var policies = await _db.PolicyRegisters.AsNoTracking().FirstOrDefaultAsync(p => p.OrganisationId == org.Id, ct);
            if (policies is not null)
            {
                rep.B1!.ConfidentialityOmissions = policies.ConfidentialityOmissions;
                rep.B1!.OmissionNote = policies.OmissionNote;
                rep.B2 = new B2Section
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
                };
            }
        }

        // B3 (emissions) from V1 activities as fallback
        var s1 = periodV1.EnvironmentalActivities.Where(a => a.Scope == EmissionScope.Scope1).Sum(a => a.CO2eKg);
        var s2 = periodV1.EnvironmentalActivities.Where(a => a.Scope == EmissionScope.Scope2).Sum(a => a.CO2eKg);
        var s3 = periodV1.EnvironmentalActivities.Where(a => a.Scope == EmissionScope.Scope3).Sum(a => a.CO2eKg);
        var total = s1 + s2 + s3;
        var carriers = periodV1.EnvironmentalActivities
            .GroupBy(a => string.IsNullOrWhiteSpace(a.Category) ? "(unspecified)" : a.Category)
            .Select(g => new B3CarrierItem { Carrier = g.Key, Co2eKg = g.Sum(x => x.CO2eKg) })
            .OrderByDescending(x => x.Co2eKg).ToList();
        rep.B3 = new B3Section
        {
            Scope1Kg = s1,
            Scope2Kg = s2,
            Scope3Kg = s3,
            TotalKg = total,
            IntensityKgPerRevenue = null,
            ByCarrier = carriers
        };

        // Include manual metrics captured against the V1 period (wizard entries)
        await AppendManualSectionMetricsAsync(rep, periodV1.Id, ct);

        // If V2 mapping exists, enrich the report with structured data
        if (periodV2Id is Guid pid)
        {
            await FullReportEnricher.PopulateFromV2Async(_db, rep, pid, ct);
            await AppendManualSectionMetricsAsync(rep, pid, ct);
        }

        return rep;
    }

    private async Task AppendManualSectionMetricsAsync(FullEsgReport rep, Guid periodId, CancellationToken ct)
    {
        var manualInputs = await _db.SectionMetricInputs.AsNoTracking()
            .Where(x => x.ReportingPeriodId == periodId)
            .ToListAsync(ct);

        if (manualInputs.Count == 0) return;

        foreach (var input in manualInputs)
        {
            if (!rep.SectionMetrics.TryGetValue(input.Section, out var set))
            {
                set = new SectionMetricSet { Section = input.Section };
                rep.SectionMetrics[input.Section] = set;
            }

            set.Metrics[input.Metric] = new MetricValue
            {
                Value = input.NumericValue,
                Text = input.TextValue,
                Unit = input.Unit,
                Notes = input.Notes,
                Source = "manual",
                Formula = string.Concat(input.Section, ".", input.Metric)
            };
        }
    }
}
