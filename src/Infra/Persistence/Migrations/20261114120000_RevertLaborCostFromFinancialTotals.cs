using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Craftsman.Infra.Persistence.Migrations;

[ExcludeFromCodeCoverage]
public partial class RevertLaborCostFromFinancialTotals : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            UPDATE financial_settlements
            SET production_cost_amount = production_cost_amount - labor_cost_amount,
                total_cost_amount = production_cost_amount - labor_cost_amount + shipping_cost_amount + discount_amount,
                margin_amount = revenue_amount - (production_cost_amount - labor_cost_amount + shipping_cost_amount + discount_amount)
            WHERE labor_cost_reconciled = TRUE
              AND labor_cost_amount > 0;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            UPDATE financial_settlements
            SET production_cost_amount = production_cost_amount + labor_cost_amount,
                total_cost_amount = production_cost_amount + labor_cost_amount + shipping_cost_amount + discount_amount,
                margin_amount = revenue_amount - (production_cost_amount + labor_cost_amount + shipping_cost_amount + discount_amount)
            WHERE labor_cost_reconciled = TRUE
              AND labor_cost_amount > 0;
            """);
    }
}
