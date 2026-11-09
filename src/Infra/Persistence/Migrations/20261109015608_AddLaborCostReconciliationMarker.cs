using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Craftsman.Infra.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLaborCostReconciliationMarker : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "labor_cost_reconciled",
                table: "financial_settlements",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "labor_cost_reconciled",
                table: "financial_settlements");
        }
    }
}
