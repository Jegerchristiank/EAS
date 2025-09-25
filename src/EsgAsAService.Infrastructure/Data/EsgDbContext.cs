using EsgAsAService.Domain.Entities;
using EsgAsAService.Infrastructure.Identity;
using EsgAsAService.Domain.Entities.Core;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace EsgAsAService.Infrastructure.Data;

public class EsgDbContext(DbContextOptions<EsgDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<ReportingPeriod> ReportingPeriods => Set<ReportingPeriod>();
    public DbSet<EnvironmentalActivity> EnvironmentalActivities => Set<EnvironmentalActivity>();
    public DbSet<Scope1Activity> Scope1Activities => Set<Scope1Activity>();
    public DbSet<Scope2Activity> Scope2Activities => Set<Scope2Activity>();
    public DbSet<Scope3Activity> Scope3Activities => Set<Scope3Activity>();
    public DbSet<SocialIndicators> SocialIndicators => Set<SocialIndicators>();
    public DbSet<GovernancePractices> GovernancePractices => Set<GovernancePractices>();
    public DbSet<EmissionFactor> EmissionFactors => Set<EmissionFactor>();

    // V2 Core domain
    public DbSet<Organisation> Organisations => Set<Organisation>();
    public DbSet<ReportingPeriodV2> ReportingPeriodsV2 => Set<ReportingPeriodV2>();
    public DbSet<DataSource> DataSources => Set<DataSource>();
    public DbSet<EmissionFactorV2> EmissionFactorsV2 => Set<EmissionFactorV2>();
    public DbSet<Unit> Units => Set<Unit>();
    public DbSet<UnitConversion> UnitConversions => Set<UnitConversion>();
    public DbSet<Activity> Activities => Set<Activity>();
    public DbSet<ScopeEntry> ScopeEntries => Set<ScopeEntry>();
    public DbSet<EvidenceDocument> EvidenceDocuments => Set<EvidenceDocument>();
    public DbSet<CalculationResult> CalculationResults => Set<CalculationResult>();
    public DbSet<Deviation> Deviations => Set<Deviation>();
    public DbSet<Approval> Approvals => Set<Approval>();
    public DbSet<ReportDraft> ReportDrafts => Set<ReportDraft>();
    public DbSet<VsmeMapping> VsmeMappings => Set<VsmeMapping>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<PeriodMapping> PeriodMappings => Set<PeriodMapping>();
    // ESG extended entities
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<Certificate> Certificates => Set<Certificate>();
    public DbSet<PolicyRegister> PolicyRegisters => Set<PolicyRegister>();
    public DbSet<WaterMeter> WaterMeters => Set<WaterMeter>();
    public DbSet<WasteManifest> WasteManifests => Set<WasteManifest>();
    public DbSet<MaterialFlow> MaterialFlows => Set<MaterialFlow>();
    public DbSet<HRHeadcount> HRHeadcounts => Set<HRHeadcount>();
    public DbSet<HRPayroll> HRPayrolls => Set<HRPayroll>();
    public DbSet<HRTraining> HRTrainings => Set<HRTraining>();
    public DbSet<SafetyIncident> SafetyIncidents => Set<SafetyIncident>();
    public DbSet<GovernanceCase> GovernanceCases => Set<GovernanceCase>();
    public DbSet<Financials> Financials => Set<Financials>();
    public DbSet<PollutionRegister> PollutionRegisters => Set<PollutionRegister>();
    public DbSet<StagingDocument> StagingDocuments => Set<StagingDocument>();
    public DbSet<StagingLine> StagingLines => Set<StagingLine>();
    public DbSet<StrategyTarget> StrategyTargets => Set<StrategyTarget>();
    public DbSet<RiskAssessment> RiskAssessments => Set<RiskAssessment>();
    public DbSet<HumanRightsAssessment> HumanRightsAssessments => Set<HumanRightsAssessment>();
    public DbSet<GovernanceOversight> GovernanceOversights => Set<GovernanceOversight>();
    public DbSet<BoardDiversity> BoardDiversities => Set<BoardDiversity>();
    public DbSet<StakeholderEngagement> StakeholderEngagements => Set<StakeholderEngagement>();
    public DbSet<ValueChainCoverage> ValueChainCoverages => Set<ValueChainCoverage>();
    public DbSet<AssuranceActivity> AssuranceActivities => Set<AssuranceActivity>();
    public DbSet<MethodologyStatement> MethodologyStatements => Set<MethodologyStatement>();
    public DbSet<SectionMetricInput> SectionMetricInputs => Set<SectionMetricInput>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<EnvironmentalActivity>()
            .HasDiscriminator<string>("ActivityType")
            .HasValue<Scope1Activity>(nameof(Scope1Activity))
            .HasValue<Scope2Activity>(nameof(Scope2Activity))
            .HasValue<Scope3Activity>(nameof(Scope3Activity));

        builder.Entity<Company>()
            .HasMany(c => c.ReportingPeriods)
            .WithOne(r => r.Company)
            .HasForeignKey(r => r.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ReportingPeriod>()
            .HasMany(r => r.EnvironmentalActivities)
            .WithOne(a => a.ReportingPeriod)
            .HasForeignKey(a => a.ReportingPeriodId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ReportingPeriod>()
            .HasOne(r => r.SocialIndicators)
            .WithOne(s => s.ReportingPeriod!)
            .HasForeignKey<SocialIndicators>(s => s.ReportingPeriodId);

        builder.Entity<ReportingPeriod>()
            .HasOne(r => r.GovernancePractices)
            .WithOne(g => g.ReportingPeriod!)
            .HasForeignKey<GovernancePractices>(g => g.ReportingPeriodId);

        builder.Entity<EmissionFactor>()
            .HasIndex(x => new { x.Source, x.Category, x.Unit });

        builder.Entity<Organisation>().HasIndex(x => new { x.Name, x.OrganizationNumber, x.CountryCode });
        builder.Entity<ReportingPeriodV2>().HasIndex(x => new { x.OrganisationId, x.Year });
        builder.Entity<EmissionFactorV2>().HasIndex(x => new { x.Country, x.Year, x.Type });
        builder.Entity<Unit>().HasIndex(x => x.Code).IsUnique();
        builder.Entity<UnitConversion>().HasIndex(x => new { x.FromUnitId, x.ToUnitId }).IsUnique();
        builder.Entity<Activity>().HasIndex(x => new { x.OrganisationId, x.ReportingPeriodId });
        builder.Entity<ScopeEntry>().HasIndex(x => x.ActivityId);
        builder.Entity<EvidenceDocument>().HasIndex(x => x.ScopeEntryId);
        builder.Entity<CalculationResult>().HasIndex(x => x.ScopeEntryId);
        // Extended indexes
        builder.Entity<Location>().HasIndex(x => new { x.OrganisationId });
        builder.Entity<WaterMeter>().HasIndex(x => new { x.OrganisationId, x.ReportingPeriodId });
        builder.Entity<WasteManifest>().HasIndex(x => new { x.OrganisationId, x.ReportingPeriodId });
        builder.Entity<MaterialFlow>().HasIndex(x => new { x.OrganisationId, x.ReportingPeriodId });
        builder.Entity<HRHeadcount>().HasIndex(x => new { x.OrganisationId, x.ReportingPeriodId });
        builder.Entity<HRPayroll>().HasIndex(x => new { x.OrganisationId, x.ReportingPeriodId });
        builder.Entity<HRTraining>().HasIndex(x => new { x.OrganisationId, x.ReportingPeriodId });
        builder.Entity<SafetyIncident>().HasIndex(x => new { x.OrganisationId, x.ReportingPeriodId });
        builder.Entity<GovernanceCase>().HasIndex(x => new { x.OrganisationId, x.ReportingPeriodId });
        builder.Entity<Financials>().HasIndex(x => new { x.OrganisationId, x.ReportingPeriodId });
        builder.Entity<PollutionRegister>().HasIndex(x => new { x.OrganisationId, x.ReportingPeriodId });
        builder.Entity<StagingDocument>().HasIndex(x => new { x.OrganisationId, x.ReportingPeriodId, x.Type });
        builder.Entity<StagingLine>().HasIndex(x => new { x.DocumentId, x.LineNo }).IsUnique();

        // Filter for active revisions by default
        builder.Entity<Organisation>().HasQueryFilter(x => x.IsActive);
        builder.Entity<ReportingPeriodV2>().HasQueryFilter(x => x.IsActive);
        builder.Entity<DataSource>().HasQueryFilter(x => x.IsActive);
        builder.Entity<EmissionFactorV2>().HasQueryFilter(x => x.IsActive);
        builder.Entity<Unit>().HasQueryFilter(x => x.IsActive);
        builder.Entity<UnitConversion>().HasQueryFilter(x => x.IsActive);
        builder.Entity<Activity>().HasQueryFilter(x => x.IsActive);
        builder.Entity<ScopeEntry>().HasQueryFilter(x => x.IsActive);
        builder.Entity<EvidenceDocument>().HasQueryFilter(x => x.IsActive);
        builder.Entity<CalculationResult>().HasQueryFilter(x => x.IsActive);
        builder.Entity<Deviation>().HasQueryFilter(x => x.IsActive);
        builder.Entity<Approval>().HasQueryFilter(x => x.IsActive);
        builder.Entity<ReportDraft>().HasQueryFilter(x => x.IsActive);
        builder.Entity<VsmeMapping>().HasQueryFilter(x => x.IsActive);
        builder.Entity<Location>().HasQueryFilter(x => x.IsActive);
        builder.Entity<Certificate>().HasQueryFilter(x => x.IsActive);
        builder.Entity<PolicyRegister>().HasQueryFilter(x => x.IsActive);
        builder.Entity<WaterMeter>().HasQueryFilter(x => x.IsActive);
        builder.Entity<WasteManifest>().HasQueryFilter(x => x.IsActive);
        builder.Entity<MaterialFlow>().HasQueryFilter(x => x.IsActive);
        builder.Entity<HRHeadcount>().HasQueryFilter(x => x.IsActive);
        builder.Entity<HRPayroll>().HasQueryFilter(x => x.IsActive);
        builder.Entity<HRTraining>().HasQueryFilter(x => x.IsActive);
        builder.Entity<SafetyIncident>().HasQueryFilter(x => x.IsActive);
        builder.Entity<GovernanceCase>().HasQueryFilter(x => x.IsActive);
        builder.Entity<Financials>().HasQueryFilter(x => x.IsActive);
        builder.Entity<PollutionRegister>().HasQueryFilter(x => x.IsActive);
        builder.Entity<StagingDocument>().HasQueryFilter(x => x.IsActive);
        builder.Entity<StagingLine>().HasQueryFilter(x => x.IsActive);
        builder.Entity<StrategyTarget>().HasQueryFilter(x => x.IsActive);
        builder.Entity<RiskAssessment>().HasQueryFilter(x => x.IsActive);
        builder.Entity<HumanRightsAssessment>().HasQueryFilter(x => x.IsActive);
        builder.Entity<GovernanceOversight>().HasQueryFilter(x => x.IsActive);
        builder.Entity<BoardDiversity>().HasQueryFilter(x => x.IsActive);
        builder.Entity<StakeholderEngagement>().HasQueryFilter(x => x.IsActive);
        builder.Entity<ValueChainCoverage>().HasQueryFilter(x => x.IsActive);
        builder.Entity<AssuranceActivity>().HasQueryFilter(x => x.IsActive);
        builder.Entity<MethodologyStatement>().HasQueryFilter(x => x.IsActive);
        builder.Entity<SectionMetricInput>().HasQueryFilter(x => x.IsActive);

        // Explicit mapping between V1 and V2 periods
        builder.Entity<PeriodMapping>().HasKey(x => x.V1PeriodId);
    }

    public override int SaveChanges()
    {
        ApplyRevisioning();
        WriteAuditLogs();
        return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyRevisioning();
        WriteAuditLogs();
        return await base.SaveChangesAsync(cancellationToken);
    }

    private void ApplyRevisioning()
    {
        foreach (var entry in ChangeTracker.Entries<RevisionedEntity>())
        {
            if (entry.State == EntityState.Modified)
            {
                // Create a new revision: clone values into a new entity with incremented version
                var current = entry.Entity;
                var newValues = Entry(current).CurrentValues.Clone().ToObject() as RevisionedEntity;
                if (newValues == null) continue;

                // mark existing as inactive, ensure only IsActive is updated
                Entry(current).Property(nameof(RevisionedEntity.IsActive)).CurrentValue = false;
                foreach (var prop in Entry(current).Properties)
                {
                    prop.IsModified = prop.Metadata.Name == nameof(RevisionedEntity.IsActive);
                }
                entry.State = EntityState.Modified;

                // insert new row as next version
                newValues.Id = Guid.NewGuid();
                newValues.Version = current.Version + 1;
                newValues.IsActive = true;
                newValues.RevisionGroupId = current.RevisionGroupId;
                Add(newValues);
            }
            else if (entry.State == EntityState.Added)
            {
                if (entry.Entity.RevisionGroupId == Guid.Empty)
                    entry.Entity.RevisionGroupId = Guid.NewGuid();
                if (entry.Entity.Version <= 0)
                    entry.Entity.Version = 1;
                entry.Entity.IsActive = true;
            }
        }
    }

    private void WriteAuditLogs()
    {
        var now = DateTimeOffset.UtcNow;
        var jsonOptions = new System.Text.Json.JsonSerializerOptions
        {
            ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles,
            WriteIndented = false
        };
        var added = ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Added)
            .Where(e => e.Entity is not AuditLog)
            .ToList();

        foreach (var e in added)
        {
            var entityType = e.Entity.GetType();
            var entityName = entityType.Name;
            Guid id = default;
            var idProp = entityType.GetProperty("Id");
            if (idProp != null)
            {
                var val = idProp.GetValue(e.Entity);
                if (val is Guid g) id = g;
            }
            var json = System.Text.Json.JsonSerializer.Serialize(e.Entity, jsonOptions);
            var hash = ComputeSha256(json);
            AuditLogs.Add(new AuditLog
            {
                Timestamp = now,
                EntityName = entityName,
                EntityId = id,
                Action = "Insert",
                PayloadJson = json,
                PayloadHash = hash
            });
        }

        var modified = ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Modified)
            .Where(e => e.Entity is not AuditLog)
            .ToList();

        foreach (var e in modified)
        {
            var entityType = e.Entity.GetType();
            var entityName = entityType.Name;
            Guid id = default;
            var idProp = entityType.GetProperty("Id");
            if (idProp != null)
            {
                var val = idProp.GetValue(e.Entity);
                if (val is Guid g) id = g;
            }

            var changes = e.Properties
                .Where(p => p.IsModified)
                .ToDictionary(p => p.Metadata.Name, p => new { original = p.OriginalValue, current = p.CurrentValue });
            var json = System.Text.Json.JsonSerializer.Serialize(new { entity = e.Entity, changes }, jsonOptions);
            var hash = ComputeSha256(json);
            AuditLogs.Add(new AuditLog
            {
                Timestamp = now,
                EntityName = entityName,
                EntityId = id,
                Action = "Update",
                PayloadJson = json,
                PayloadHash = hash
            });
        }
    }

    private static string ComputeSha256(string input)
    {
        using var sha = System.Security.Cryptography.SHA256.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(input);
        var hash = sha.ComputeHash(bytes);
        return Convert.ToHexString(hash);
    }
}
