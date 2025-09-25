using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EsgAsAService.Application.Abstractions;
using EsgAsAService.Application.Models;
using EsgAsAService.Domain.Entities;
using EsgAsAService.Domain.Entities.Core;
using Microsoft.EntityFrameworkCore;

namespace EsgAsAService.Infrastructure.Data;

public class EsgDataService : IEsgDataService
{
    private readonly EsgDbContext _db;

    public EsgDataService(EsgDbContext db)
    {
        _db = db;
    }

    public async Task<Company> UpsertCompanyAsync(Company company, CancellationToken ct = default)
    {
        if (await _db.Companies.AnyAsync(x => x.Id == company.Id, ct))
            _db.Companies.Update(company);
        else
            await _db.Companies.AddAsync(company, ct);

        await _db.SaveChangesAsync(ct);
        return company;
    }

    public async Task<ReportingPeriod> UpsertReportingPeriodAsync(ReportingPeriod period, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(period);
        // Helper: apply changes from incoming 'period' to a tracked 'existing' entity
        static void ApplyChangesToExisting(EsgDbContext db, ReportingPeriod existing, ReportingPeriod period, bool forceReplaceChildren = false)
        {
            // Update scalar fields
            existing.CompanyId = period.CompanyId;
            existing.StartDate = period.StartDate;
            existing.EndDate = period.EndDate;
            existing.Year = period.Year;
            existing.IsCalendarYear = period.IsCalendarYear;

            // Optionally force replace single-child entities to avoid stale tracked rows
            if (forceReplaceChildren)
            {
                // Detach existing children so EF will insert fresh entities if provided
                if (existing.SocialIndicators is not null)
                {
                    db.Entry(existing.SocialIndicators).State = EntityState.Detached;
                    existing.SocialIndicators = null;
                }
                if (existing.GovernancePractices is not null)
                {
                    db.Entry(existing.GovernancePractices).State = EntityState.Detached;
                    existing.GovernancePractices = null;
                }
            }

            // Upsert SocialIndicators
            if (period.SocialIndicators is not null)
            {
                period.SocialIndicators.ReportingPeriodId = existing.Id;
                if (existing.SocialIndicators is null)
                {
                    if (period.SocialIndicators.Id == Guid.Empty) period.SocialIndicators.Id = Guid.NewGuid();
                    existing.SocialIndicators = period.SocialIndicators;
                    db.Entry(existing.SocialIndicators).State = EntityState.Added;
                }
                else
                {
                    existing.SocialIndicators.EmployeeCount = period.SocialIndicators.EmployeeCount;
                    existing.SocialIndicators.PercentFemaleEmployees = period.SocialIndicators.PercentFemaleEmployees;
                    existing.SocialIndicators.TotalHoursWorked = period.SocialIndicators.TotalHoursWorked;
                    existing.SocialIndicators.TrainingHours = period.SocialIndicators.TrainingHours;
                    existing.SocialIndicators.LostTimeInjuries = period.SocialIndicators.LostTimeInjuries;
                    db.Entry(existing.SocialIndicators).State = EntityState.Modified;
                }
            }

            // Upsert GovernancePractices
            if (period.GovernancePractices is not null)
            {
                period.GovernancePractices.ReportingPeriodId = existing.Id;
                if (existing.GovernancePractices is null)
                {
                    if (period.GovernancePractices.Id == Guid.Empty) period.GovernancePractices.Id = Guid.NewGuid();
                    existing.GovernancePractices = period.GovernancePractices;
                    db.Entry(existing.GovernancePractices).State = EntityState.Added;
                }
                else
                {
                    existing.GovernancePractices.PercentFemaleBoard = period.GovernancePractices.PercentFemaleBoard;
                    existing.GovernancePractices.AntiCorruptionPolicy = period.GovernancePractices.AntiCorruptionPolicy;
                    existing.GovernancePractices.DataPrivacyPolicy = period.GovernancePractices.DataPrivacyPolicy;
                    existing.GovernancePractices.WhistleblowerPolicy = period.GovernancePractices.WhistleblowerPolicy;
                    existing.GovernancePractices.Notes = period.GovernancePractices.Notes;
                    db.Entry(existing.GovernancePractices).State = EntityState.Modified;
                }
            }

            // Replace EnvironmentalActivities: remove existing and add fresh rows to avoid concurrency issues
            if (existing.EnvironmentalActivities?.Count > 0)
            {
                // Materialize to avoid 'Collection was modified' during RemoveRange enumeration
                var oldActivities = existing.EnvironmentalActivities.ToList();
                db.EnvironmentalActivities.RemoveRange(oldActivities);
            }
            if (period.EnvironmentalActivities is not null)
            {
                foreach (var a in period.EnvironmentalActivities)
                {
                    // Ensure new identity and linkage; always insert
                    a.Id = Guid.NewGuid();
                    a.ReportingPeriodId = existing.Id;
                    db.Entry(a).State = EntityState.Added;
                }
            }
        }

        // Detach any local tracked instance with same key to avoid state conflicts
        var localTracked = _db.ChangeTracker.Entries<ReportingPeriod>()
            .FirstOrDefault(e => e.Entity.Id == period.Id);
        if (localTracked is not null)
        {
            localTracked.State = EntityState.Detached;
        }

        var existing = await _db.ReportingPeriods
            .Include(r => r.EnvironmentalActivities)
            .Include(r => r.SocialIndicators)
            .Include(r => r.GovernancePractices)
            .AsTracking()
            .FirstOrDefaultAsync(x => x.Id == period.Id, ct);

        if (existing is null)
        {
            // New period with full graph
            await _db.ReportingPeriods.AddAsync(period, ct);
        }
        else
        {
            ApplyChangesToExisting(_db, existing, period);
        }

        // Try saving with a small retry loop to handle transient optimistic concurrency
        const int maxAttempts = 3;
        var forceReplaceChildren = false;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                await _db.SaveChangesAsync(ct);
                return existing ?? period;
            }
            catch (DbUpdateConcurrencyException) when (attempt < maxAttempts)
            {
                // Clear tracked state and reload the latest from DB, then reapply changes and retry
                _db.ChangeTracker.Clear();
                existing = await _db.ReportingPeriods
                    .Include(r => r.EnvironmentalActivities)
                    .Include(r => r.SocialIndicators)
                    .Include(r => r.GovernancePractices)
                    .AsTracking()
                    .FirstOrDefaultAsync(x => x.Id == period.Id, ct);

                if (existing is null)
                {
                    // The row disappeared while trying to save. Instead of rethrowing and crashing the UI,
                    // return the latest database state (if any) or the incoming period so the caller can
                    // decide how to proceed without an unhandled exception.
                    _db.ChangeTracker.Clear();
                    var latest = await _db.ReportingPeriods
                        .Include(r => r.EnvironmentalActivities)
                        .Include(r => r.SocialIndicators)
                        .Include(r => r.GovernancePractices)
                        .FirstOrDefaultAsync(x => x.Id == period.Id, ct);

                    return latest ?? period;
                }
                // On concurrency retry, force children replacement to avoid stale missing rows
                forceReplaceChildren = true;
                ApplyChangesToExisting(_db, existing, period, forceReplaceChildren);
                // loop and retry
            }
            catch (DbUpdateConcurrencyException)
            {
                // Final fallback: swallow the exception and return the most recent state
                _db.ChangeTracker.Clear();
                var latest = await _db.ReportingPeriods
                    .Include(r => r.EnvironmentalActivities)
                    .Include(r => r.SocialIndicators)
                    .Include(r => r.GovernancePractices)
                    .FirstOrDefaultAsync(x => x.Id == period.Id, ct);
                return latest ?? period;
            }
        }

        // Should not reach here, but return the incoming object as a safe default
        return period;
    }

    public async Task<IReadOnlyList<SectionMetricValue>> GetSectionMetricsAsync(Guid reportingPeriodId, CancellationToken ct = default)
    {
        return await _db.SectionMetricInputs.AsNoTracking()
            .Where(x => x.ReportingPeriodId == reportingPeriodId)
            .Select(x => new SectionMetricValue(
                x.Section,
                x.Metric,
                x.NumericValue,
                x.TextValue,
                x.Unit,
                x.Notes))
            .ToListAsync(ct);
    }

    public async Task SaveSectionMetricsAsync(Guid reportingPeriodId, Guid organisationId, IReadOnlyCollection<SectionMetricValue> values, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(values);

        var existing = await _db.SectionMetricInputs
            .Where(x => x.ReportingPeriodId == reportingPeriodId)
            .ToListAsync(ct);

        var comparer = new SectionMetricKeyComparer();
        var incomingKeys = new HashSet<(string Section, string Metric)>(values
            .Where(v => !string.IsNullOrWhiteSpace(v.Section) && !string.IsNullOrWhiteSpace(v.Metric))
            .Select(v => (v.Section.Trim(), v.Metric.Trim())), comparer);

        foreach (var entry in existing.ToList())
        {
            if (!incomingKeys.Contains((entry.Section, entry.Metric)))
            {
                _db.SectionMetricInputs.Remove(entry);
            }
        }

        var existingLookup = existing.ToDictionary(e => (e.Section, e.Metric), comparer);

        foreach (var value in values)
        {
            var section = value.Section?.Trim();
            var metric = value.Metric?.Trim();
            if (string.IsNullOrWhiteSpace(section) || string.IsNullOrWhiteSpace(metric))
            {
                continue;
            }

            var hasContent = value.NumericValue.HasValue || !string.IsNullOrWhiteSpace(value.TextValue) || !string.IsNullOrWhiteSpace(value.Unit) || !string.IsNullOrWhiteSpace(value.Notes);
            if (!hasContent)
            {
                continue;
            }

            if (!existingLookup.TryGetValue((section, metric), out var entity))
            {
                entity = new SectionMetricInput
                {
                    OrganisationId = organisationId,
                    ReportingPeriodId = reportingPeriodId,
                    Section = section,
                    Metric = metric
                };
                await _db.SectionMetricInputs.AddAsync(entity, ct);
                existingLookup[(section, metric)] = entity;
            }

            entity.NumericValue = value.NumericValue;
            entity.TextValue = string.IsNullOrWhiteSpace(value.TextValue) ? null : value.TextValue;
            entity.Unit = string.IsNullOrWhiteSpace(value.Unit) ? null : value.Unit;
            entity.Notes = string.IsNullOrWhiteSpace(value.Notes) ? null : value.Notes;
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task<ReportingPeriod?> GetReportingPeriodAsync(Guid id, CancellationToken ct = default)
    {
        return await _db.ReportingPeriods
            .Include(r => r.Company)
            .Include(r => r.EnvironmentalActivities)
            .Include(r => r.SocialIndicators)
            .Include(r => r.GovernancePractices)
            .FirstOrDefaultAsync(r => r.Id == id, ct);
    }

    public async Task<Guid?> GetMappedV2PeriodAsync(Guid v1PeriodId, CancellationToken ct = default)
    {
        var map = await _db.PeriodMappings.AsNoTracking().FirstOrDefaultAsync(m => m.V1PeriodId == v1PeriodId, ct);
        return map?.V2PeriodId;
    }

    public async Task SavePeriodMappingAsync(Guid v1PeriodId, Guid v2PeriodId, CancellationToken ct = default)
    {
        var existing = await _db.PeriodMappings.FirstOrDefaultAsync(x => x.V1PeriodId == v1PeriodId, ct);
        if (existing is null)
        {
            await _db.PeriodMappings.AddAsync(new PeriodMapping { V1PeriodId = v1PeriodId, V2PeriodId = v2PeriodId }, ct);
        }
        else
        {
            existing.V2PeriodId = v2PeriodId;
            _db.PeriodMappings.Update(existing);
        }
        await _db.SaveChangesAsync(ct);
    }

    private async Task<(Guid OrgId, Guid PeriodId)> EnsureV2Refs(Guid v2PeriodId, CancellationToken ct)
    {
        var v2 = await _db.ReportingPeriodsV2.AsNoTracking().FirstOrDefaultAsync(p => p.Id == v2PeriodId, ct)
                 ?? throw new InvalidOperationException("v2_period_not_found");
        return (v2.OrganisationId, v2.Id);
    }

    public async Task AddWaterAsync(Guid v2PeriodId, double intakeM3, double? dischargeM3, CancellationToken ct = default)
    {
        var (orgId, pid) = await EnsureV2Refs(v2PeriodId, ct);
        await _db.WaterMeters.AddAsync(new WaterMeter
        {
            OrganisationId = orgId,
            ReportingPeriodId = pid,
            IntakeM3 = intakeM3,
            DischargeM3 = dischargeM3
        }, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task AddWasteAsync(Guid v2PeriodId, string eakCode, double quantityKg, string disposition, CancellationToken ct = default)
    {
        var (orgId, pid) = await EnsureV2Refs(v2PeriodId, ct);
        await _db.WasteManifests.AddAsync(new WasteManifest
        {
            OrganisationId = orgId,
            ReportingPeriodId = pid,
            EakCode = eakCode,
            QuantityKg = quantityKg,
            Disposition = disposition
        }, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task AddMaterialAsync(Guid v2PeriodId, string material, double quantityTonnes, CancellationToken ct = default)
    {
        var (orgId, pid) = await EnsureV2Refs(v2PeriodId, ct);
        await _db.MaterialFlows.AddAsync(new MaterialFlow
        {
            OrganisationId = orgId,
            ReportingPeriodId = pid,
            Material = material,
            QuantityTonnes = quantityTonnes
        }, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpsertFinancialsAsync(Guid v2PeriodId, double revenue, string? currency, CancellationToken ct = default)
    {
        var (orgId, pid) = await EnsureV2Refs(v2PeriodId, ct);
        var fin = await _db.Financials.FirstOrDefaultAsync(f => f.ReportingPeriodId == pid, ct);
        if (fin is null)
        {
            fin = new Financials { OrganisationId = orgId, ReportingPeriodId = pid, Revenue = revenue, Currency = currency };
            await _db.Financials.AddAsync(fin, ct);
        }
        else
        {
            fin.Revenue = revenue;
            fin.Currency = currency;
            _db.Financials.Update(fin);
        }
        await _db.SaveChangesAsync(ct);
    }

    private sealed class SectionMetricKeyComparer : IEqualityComparer<(string Section, string Metric)>
    {
        public bool Equals((string Section, string Metric) x, (string Section, string Metric) y)
            => string.Equals(x.Section, y.Section, StringComparison.OrdinalIgnoreCase)
               && string.Equals(x.Metric, y.Metric, StringComparison.OrdinalIgnoreCase);

        public int GetHashCode((string Section, string Metric) obj)
        {
            var sectionHash = obj.Section is null ? 0 : StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Section);
            var metricHash = obj.Metric is null ? 0 : StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Metric);
            return HashCode.Combine(sectionHash, metricHash);
        }
    }
}
