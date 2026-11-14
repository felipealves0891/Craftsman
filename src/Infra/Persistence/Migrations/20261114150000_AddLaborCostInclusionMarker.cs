using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Craftsman.Infra.Persistence.Migrations;

[ExcludeFromCodeCoverage]
public partial class AddLaborCostInclusionMarker : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "labor_cost_included_in_production",
            table: "financial_settlements",
            type: "boolean",
            nullable: false,
            defaultValue: true);

        migrationBuilder.Sql("""
            UPDATE financial_settlements
            SET production_cost_amount = CASE
                    WHEN labor_cost_amount > 0 THEN production_cost_amount - labor_cost_amount
                    ELSE production_cost_amount
                END,
                total_cost_amount = CASE
                    WHEN labor_cost_amount > 0 THEN production_cost_amount - labor_cost_amount + shipping_cost_amount + discount_amount
                    ELSE production_cost_amount + shipping_cost_amount + discount_amount
                END,
                margin_amount = revenue_amount - CASE
                    WHEN labor_cost_amount > 0 THEN production_cost_amount - labor_cost_amount + shipping_cost_amount + discount_amount
                    ELSE production_cost_amount + shipping_cost_amount + discount_amount
                END,
                labor_cost_included_in_production = FALSE;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            UPDATE financial_settlements
            SET production_cost_amount = production_cost_amount + labor_cost_amount,
                total_cost_amount = production_cost_amount + labor_cost_amount + shipping_cost_amount + discount_amount,
                margin_amount = revenue_amount - (production_cost_amount + labor_cost_amount + shipping_cost_amount + discount_amount)
            WHERE labor_cost_included_in_production = FALSE
              AND labor_cost_amount > 0;
            """);

        migrationBuilder.DropColumn(
            name: "labor_cost_included_in_production",
            table: "financial_settlements");
    }
}
