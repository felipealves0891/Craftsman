using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Craftsman.Infra.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLaborCostInclusionMarker : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "labor_cost_included_in_production",
                table: "financial_settlements",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "labor_cost_included_in_production",
                table: "financial_settlements");
        }
    }
}
