using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EsgAsAService.Infrastructure.Migrations
{
    public partial class Initial20250911V2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            ArgumentNullException.ThrowIfNull(migrationBuilder);

            migrationBuilder.CreateTable(
                name: "Organisations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    RevisionGroupId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Industry = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    CountryCode = table.Column<string>(type: "TEXT", maxLength: 2, nullable: true),
                    OrganizationNumber = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
                },
                constraints: table => { table.PrimaryKey("PK_Organisations", x => x.Id); }
            );

            migrationBuilder.CreateTable(
                name: "ReportingPeriodsV2",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    RevisionGroupId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    OrganisationId = table.Column<Guid>(type: "TEXT", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Year = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table => { table.PrimaryKey("PK_ReportingPeriodsV2", x => x.Id); }
            );

            migrationBuilder.CreateTable(
                name: "DataSources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    RevisionGroupId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Url = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table => { table.PrimaryKey("PK_DataSources", x => x.Id); }
            );

            migrationBuilder.CreateTable(
                name: "Units",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    RevisionGroupId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    Code = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true)
                },
                constraints: table => { table.PrimaryKey("PK_Units", x => x.Id); }
            );

            migrationBuilder.CreateTable(
                name: "UnitConversions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    RevisionGroupId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    FromUnitId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ToUnitId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Factor = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table => { table.PrimaryKey("PK_UnitConversions", x => x.Id); }
            );

            migrationBuilder.CreateTable(
                name: "EmissionFactorsV2",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    RevisionGroupId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    Country = table.Column<string>(type: "TEXT", maxLength: 2, nullable: false),
                    Year = table.Column<int>(type: "INTEGER", nullable: false),
                    Type = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    UnitId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Value = table.Column<double>(type: "REAL", nullable: false),
                    ValidFrom = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    ValidTo = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    DataSourceId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table => { table.PrimaryKey("PK_EmissionFactorsV2", x => x.Id); }
            );

            migrationBuilder.CreateTable(
                name: "Activities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    RevisionGroupId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    OrganisationId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ReportingPeriodId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Category = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ActivityDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    Quantity = table.Column<double>(type: "REAL", nullable: false),
                    UnitId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table => { table.PrimaryKey("PK_Activities", x => x.Id); }
            );

            migrationBuilder.CreateTable(
                name: "ScopeEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    RevisionGroupId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    ActivityId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Scope = table.Column<int>(type: "INTEGER", nullable: false),
                    EmissionFactorId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Adjustment = table.Column<double>(type: "REAL", nullable: true)
                },
                constraints: table => { table.PrimaryKey("PK_ScopeEntries", x => x.Id); }
            );

            migrationBuilder.CreateTable(
                name: "EvidenceDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    RevisionGroupId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    ScopeEntryId = table.Column<Guid>(type: "TEXT", nullable: false),
                    FileName = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    BlobUrl = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table => { table.PrimaryKey("PK_EvidenceDocuments", x => x.Id); }
            );

            migrationBuilder.CreateTable(
                name: "CalculationResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    RevisionGroupId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    ScopeEntryId = table.Column<Guid>(type: "TEXT", nullable: false),
                    QuantityNormalized = table.Column<double>(type: "REAL", nullable: false),
                    Factor = table.Column<double>(type: "REAL", nullable: false),
                    Adjustment = table.Column<double>(type: "REAL", nullable: false),
                    Co2eKg = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table => { table.PrimaryKey("PK_CalculationResults", x => x.Id); }
            );

            migrationBuilder.CreateTable(
                name: "Deviations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    RevisionGroupId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    ScopeEntryId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Reason = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false)
                },
                constraints: table => { table.PrimaryKey("PK_Deviations", x => x.Id); }
            );

            migrationBuilder.CreateTable(
                name: "Approvals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    RevisionGroupId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    ReportingPeriodId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Comment = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true)
                },
                constraints: table => { table.PrimaryKey("PK_Approvals", x => x.Id); }
            );

            migrationBuilder.CreateTable(
                name: "ReportDrafts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    RevisionGroupId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    ReportingPeriodId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Json = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table => { table.PrimaryKey("PK_ReportDrafts", x => x.Id); }
            );

            migrationBuilder.CreateTable(
                name: "VsmeMappings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    RevisionGroupId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    Json = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table => { table.PrimaryKey("PK_VsmeMappings", x => x.Id); }
            );

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    EntityName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    EntityId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Action = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    PayloadHash = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    PayloadJson = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table => { table.PrimaryKey("PK_AuditLogs", x => x.Id); }
            );
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            ArgumentNullException.ThrowIfNull(migrationBuilder);

            migrationBuilder.DropTable(name: "AuditLogs");
            migrationBuilder.DropTable(name: "VsmeMappings");
            migrationBuilder.DropTable(name: "ReportDrafts");
            migrationBuilder.DropTable(name: "Approvals");
            migrationBuilder.DropTable(name: "Deviations");
            migrationBuilder.DropTable(name: "CalculationResults");
            migrationBuilder.DropTable(name: "EvidenceDocuments");
            migrationBuilder.DropTable(name: "ScopeEntries");
            migrationBuilder.DropTable(name: "Activities");
            migrationBuilder.DropTable(name: "EmissionFactorsV2");
            migrationBuilder.DropTable(name: "UnitConversions");
            migrationBuilder.DropTable(name: "Units");
            migrationBuilder.DropTable(name: "DataSources");
            migrationBuilder.DropTable(name: "ReportingPeriodsV2");
            migrationBuilder.DropTable(name: "Organisations");
        }
    }
}

#nullable restore

