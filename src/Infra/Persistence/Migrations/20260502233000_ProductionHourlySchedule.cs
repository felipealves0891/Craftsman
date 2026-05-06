using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Craftsman.Infra.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ProductionHourlySchedule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "production_duration_hours",
                table: "production_tasks",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "planned_start_at",
                table: "production_tasks",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "NOW()");

            migrationBuilder.Sql("""
                UPDATE production_tasks
                SET planned_start_at = planned_at,
                    production_duration_hours = 1
                """);

            migrationBuilder.CreateIndex(
                name: "IX_production_tasks_planned_start_at",
                table: "production_tasks",
                column: "planned_start_at");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_production_tasks_planned_start_at",
                table: "production_tasks");

            migrationBuilder.DropColumn(
                name: "planned_start_at",
                table: "production_tasks");

            migrationBuilder.DropColumn(
                name: "production_duration_hours",
                table: "production_tasks");
        }
    }
}
