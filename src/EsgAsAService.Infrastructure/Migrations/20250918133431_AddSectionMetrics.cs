using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EsgAsAService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSectionMetrics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SectionMetricInputs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    OrganisationId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ReportingPeriodId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Section = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Metric = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    NumericValue = table.Column<double>(type: "REAL", nullable: true),
                    TextValue = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    Unit = table.Column<string>(type: "TEXT", maxLength: 16, nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    RevisionGroupId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SectionMetricInputs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SectionMetricInputs_Period_Section_Metric",
                table: "SectionMetricInputs",
                columns: new[] { "ReportingPeriodId", "Section", "Metric" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SectionMetricInputs_Period_Section_Metric",
                table: "SectionMetricInputs");

            migrationBuilder.DropTable(
                name: "SectionMetricInputs");
        }
    }
}
