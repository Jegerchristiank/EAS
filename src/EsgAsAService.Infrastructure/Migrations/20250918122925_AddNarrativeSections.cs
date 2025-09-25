using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EsgAsAService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNarrativeSections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AssuranceActivities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    OrganisationId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ReportingPeriodId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Provider = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    AssuranceLevel = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Scope = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    AssuranceDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    Summary = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    IsIndependent = table.Column<bool>(type: "INTEGER", nullable: true),
                    RevisionGroupId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssuranceActivities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BoardDiversities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    OrganisationId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ReportingPeriodId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PercentFemale = table.Column<double>(type: "REAL", nullable: true),
                    PercentMale = table.Column<double>(type: "REAL", nullable: true),
                    PercentOther = table.Column<double>(type: "REAL", nullable: true),
                    PercentIndependent = table.Column<double>(type: "REAL", nullable: true),
                    DiversityPolicy = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    SelectionProcess = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    RevisionGroupId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BoardDiversities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GovernanceOversights",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    OrganisationId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ReportingPeriodId = table.Column<Guid>(type: "TEXT", nullable: false),
                    BoardOversight = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    ManagementResponsibilities = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Incentives = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    ClimateExpertOnBoard = table.Column<bool>(type: "INTEGER", nullable: true),
                    RevisionGroupId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GovernanceOversights", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HumanRightsAssessments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    OrganisationId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ReportingPeriodId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PolicyExists = table.Column<bool>(type: "INTEGER", nullable: false),
                    DueDiligenceInPlace = table.Column<bool>(type: "INTEGER", nullable: false),
                    HighRiskAreas = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Remediation = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    TrainingProvided = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    RevisionGroupId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HumanRightsAssessments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MethodologyStatements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    OrganisationId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ReportingPeriodId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ReportingBoundary = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    ConsolidationApproach = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    EmissionFactorSources = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    EstimationApproach = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    MaterialityThreshold = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    RevisionGroupId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MethodologyStatements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RiskAssessments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    OrganisationId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ReportingPeriodId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Process = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    ClimateRisks = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Opportunities = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    TimeHorizon = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Mitigations = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    RevisionGroupId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RiskAssessments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StakeholderEngagements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    OrganisationId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ReportingPeriodId = table.Column<Guid>(type: "TEXT", nullable: false),
                    StakeholderGroups = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    EngagementProcess = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    KeyTopics = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    WorkerRepresentation = table.Column<bool>(type: "INTEGER", nullable: true),
                    RevisionGroupId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StakeholderEngagements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StrategyTargets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    OrganisationId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ReportingPeriodId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Summary = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    ShortTermTarget = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    LongTermTarget = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    EmissionReductionTargetPct = table.Column<double>(type: "REAL", nullable: true),
                    TargetYear = table.Column<int>(type: "INTEGER", nullable: true),
                    InvestmentPlan = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Progress = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    RevisionGroupId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StrategyTargets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ValueChainCoverages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    OrganisationId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ReportingPeriodId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UpstreamCoverage = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    DownstreamCoverage = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Scope3Categories = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    DataGaps = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    RevisionGroupId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ValueChainCoverages", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AssuranceActivities_ReportingPeriodId",
                table: "AssuranceActivities",
                column: "ReportingPeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_BoardDiversities_ReportingPeriodId",
                table: "BoardDiversities",
                column: "ReportingPeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_GovernanceOversights_ReportingPeriodId",
                table: "GovernanceOversights",
                column: "ReportingPeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_HumanRightsAssessments_ReportingPeriodId",
                table: "HumanRightsAssessments",
                column: "ReportingPeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_MethodologyStatements_ReportingPeriodId",
                table: "MethodologyStatements",
                column: "ReportingPeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_RiskAssessments_ReportingPeriodId",
                table: "RiskAssessments",
                column: "ReportingPeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_StakeholderEngagements_ReportingPeriodId",
                table: "StakeholderEngagements",
                column: "ReportingPeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_StrategyTargets_ReportingPeriodId",
                table: "StrategyTargets",
                column: "ReportingPeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_ValueChainCoverages_ReportingPeriodId",
                table: "ValueChainCoverages",
                column: "ReportingPeriodId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssuranceActivities");

            migrationBuilder.DropTable(
                name: "BoardDiversities");

            migrationBuilder.DropTable(
                name: "GovernanceOversights");

            migrationBuilder.DropTable(
                name: "HumanRightsAssessments");

            migrationBuilder.DropTable(
                name: "MethodologyStatements");

            migrationBuilder.DropTable(
                name: "RiskAssessments");

            migrationBuilder.DropTable(
                name: "StakeholderEngagements");

            migrationBuilder.DropTable(
                name: "StrategyTargets");

            migrationBuilder.DropTable(
                name: "ValueChainCoverages");
        }
    }
}
