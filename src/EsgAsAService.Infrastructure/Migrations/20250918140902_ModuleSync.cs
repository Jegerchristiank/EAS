using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EsgAsAService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ModuleSync : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_SectionMetricInputs_Period_Section_Metric\";");
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_SectionMetricInputs_ReportingPeriodId_Section_Metric\";");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_SectionMetricInputs_Period_Section_Metric",
                table: "SectionMetricInputs",
                columns: new[] { "ReportingPeriodId", "Section", "Metric" },
                unique: true);
        }
    }
}
