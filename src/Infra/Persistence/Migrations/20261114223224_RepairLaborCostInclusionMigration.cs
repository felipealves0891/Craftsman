using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Craftsman.Infra.Persistence.Migrations
{
    /// <inheritdoc />
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public partial class RepairLaborCostInclusionMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                WITH material_costs AS (
                    SELECT
                        settlement.id,
                        COALESCE(SUM(stock_movement.quantity * stock_movement.unit_cost_amount), 0)::numeric(18,2) AS material_cost
                    FROM financial_settlements AS settlement
                    LEFT JOIN production_tasks AS production_task
                        ON production_task.order_id = settlement.order_id
                    LEFT JOIN stock_movements AS stock_movement
                        ON stock_movement.business_reference = production_task.id::text
                    GROUP BY settlement.id
                )
                UPDATE financial_settlements AS settlement
                SET production_cost_amount = material_cost.material_cost,
                    total_cost_amount = material_cost.material_cost + settlement.shipping_cost_amount + settlement.discount_amount,
                    margin_amount = settlement.revenue_amount - (material_cost.material_cost + settlement.shipping_cost_amount + settlement.discount_amount)
                FROM material_costs AS material_cost
                WHERE settlement.id = material_cost.id;
                """);

            migrationBuilder.DropColumn(
                name: "labor_cost_reconciled",
                table: "financial_settlements");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "labor_cost_reconciled",
                table: "financial_settlements",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql("""
                UPDATE financial_settlements
                SET labor_cost_reconciled = labor_cost_amount > 0;
                """);

            migrationBuilder.Sql("""
                WITH material_costs AS (
                    SELECT
                        settlement.id,
                        COALESCE(SUM(stock_movement.quantity * stock_movement.unit_cost_amount), 0)::numeric(18,2) AS material_cost
                    FROM financial_settlements AS settlement
                    LEFT JOIN production_tasks AS production_task
                        ON production_task.order_id = settlement.order_id
                    LEFT JOIN stock_movements AS stock_movement
                        ON stock_movement.business_reference = production_task.id::text
                    GROUP BY settlement.id
                )
                UPDATE financial_settlements AS settlement
                SET production_cost_amount = material_cost.material_cost + settlement.labor_cost_amount,
                    total_cost_amount = material_cost.material_cost + settlement.labor_cost_amount + settlement.shipping_cost_amount + settlement.discount_amount,
                    margin_amount = settlement.revenue_amount - (material_cost.material_cost + settlement.labor_cost_amount + settlement.shipping_cost_amount + settlement.discount_amount)
                FROM material_costs AS material_cost
                WHERE settlement.id = material_cost.id;
                """);
        }
    }
}
