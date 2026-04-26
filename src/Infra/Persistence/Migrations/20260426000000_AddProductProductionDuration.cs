using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Craftsman.Infra.Persistence.Migrations;

public partial class AddProductProductionDuration : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "production_duration_days",
            table: "products",
            type: "integer",
            nullable: false,
            defaultValue: 1);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "production_duration_days",
            table: "products");
    }
}
